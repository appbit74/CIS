using System;
using System.DirectoryServices.AccountManagement; // หัวใจสำคัญ
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CIS.Models;

namespace CIS.Services
{
    public interface IAdService
    {
        string CreateAdUser(EmployeeProfile employee);
        void UpdateAdUser(string username, EmployeeProfile updatedData); // แก้ไข
        void ToggleDisableUser(string username, bool disable); // ปิด/เปิด การใช้งาน (แทนการลบ)
        void DeleteAdUser(string username); // ลบถาวร
        string GetCitizenIdByUsername(string username);
    }

    public class AdService : IAdService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AdService> _logger;

        public AdService(IConfiguration config, ILogger<AdService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public string CreateAdUser(EmployeeProfile employee)
        {
            string domain = _config["ActiveDirectory:Domain"];
            string container = _config["ActiveDirectory:Container"];
            string defaultPassword = _config["ActiveDirectory:DefaultPassword"];

            try
            {
                // เชื่อมต่อกับ Domain Controller
                // หมายเหตุ: เครื่อง Web Server ต้อง Join Domain หรือระบุ User/Pass ใน ContextOptions
                using (var context = new PrincipalContext(ContextType.Domain, domain, container))
                {
                    // ตรวจสอบก่อนว่า User ซ้ำไหม
                    var existingUser = UserPrincipal.FindByIdentity(context, employee.GeneratedUsername);
                    if (existingUser != null)
                    {
                        throw new Exception($"Username '{employee.GeneratedUsername}' มีอยู่ในระบบ AD แล้ว");
                    }

                    // สร้าง User ใหม่
                    using (var user = new UserPrincipal(context))
                    {
                        user.SamAccountName = employee.GeneratedUsername;
                        user.UserPrincipalName = $"{employee.GeneratedUsername}@{domain}";

                        // Map ข้อมูลจาก Database -> AD
                        user.GivenName = employee.FirstNameEN;
                        user.Surname = employee.LastNameEN;
                        user.DisplayName = $"{employee.FirstNameTH} {employee.LastNameTH}";
                        user.Description = $"{employee.Position} - {employee.Division}";
                        user.EmailAddress = employee.PersonalEmail; // หรือจะใช้อีเมลองค์กรก็ได้
                        user.EmployeeId = employee.CitizenId; // หรือรหัสพนักงาน

                        // ตั้งรหัสผ่านและเปิดใช้งาน
                        user.SetPassword(defaultPassword);
                        user.Enabled = true;
                        user.PasswordNeverExpires = false;
                        user.ExpirePasswordNow(); // บังคับเปลี่ยนรหัสตอน Login ครั้งแรก

                        user.Save(); // *** สั่งบันทึกลง AD จริง ***
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating AD user");
                throw; // ส่ง Error กลับไปให้ Controller จัดการ
            }
        }
        // 1. แก้ไขข้อมูลใน AD
        public void UpdateAdUser(string username, EmployeeProfile updatedData)
        {
            // Setup Context เหมือนเดิม
            using (var context = GetPrincipalContext())
            {
                var user = UserPrincipal.FindByIdentity(context, username);
                if (user != null)
                {
                    user.GivenName = updatedData.FirstNameEN;
                    user.Surname = updatedData.LastNameEN;
                    user.DisplayName = $"{updatedData.FirstNameTH} {updatedData.LastNameTH}";
                    user.Description = $"{updatedData.Position} - {updatedData.Division}";
                    user.Save(); // บันทึก
                }
            }
        }

        // 2. ปิดการใช้งาน (แนะนำใช้วิธีนี้แทนการลบจริง)
        public void ToggleDisableUser(string username, bool disable)
        {
            using (var context = GetPrincipalContext())
            {
                var user = UserPrincipal.FindByIdentity(context, username);
                if (user != null)
                {
                    user.Enabled = !disable; // ถ้า disable=true คือ enabled=false
                    user.Save();
                }
            }
        }

        // 3. ลบถาวร (อันตราย! ต้องระวัง)
        public void DeleteAdUser(string username)
        {
            using (var context = GetPrincipalContext())
            {
                var user = UserPrincipal.FindByIdentity(context, username);
                if (user != null)
                {
                    user.Delete(); // หายวับไปเลย
                }
            }
        }

        // [เพิ่มใหม่] Implementation
        public string GetCitizenIdByUsername(string username)
        {
            try
            {
                // ตัด Domain ออกถ้ามี (เช่น "CRIMCAD\User" -> "User")
                if (username.Contains("\\"))
                {
                    username = username.Split('\\')[1];
                }

                using (var context = GetPrincipalContext())
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user != null)
                    {
                        // ดึงค่า employeeID ซึ่งเราตกลงกันว่าเก็บ CitizenId ไว้ในนี้
                        return user.EmployeeId;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching AD user info for {username}");
            }
            return null; // หาไม่เจอ หรือ Error
        }

        // Helper function เพื่อลดโค้ดซ้ำ
        private PrincipalContext GetPrincipalContext()
        {
            string domain = _config["ActiveDirectory:Domain"];
            string container = _config["ActiveDirectory:Container"];
            string adminUser = _config["ActiveDirectory:AdminUsername"];
            string adminPass = _config["ActiveDirectory:AdminPassword"];
            return new PrincipalContext(ContextType.Domain, domain, container, adminUser, adminPass);
        }
    }
}