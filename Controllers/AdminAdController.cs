using CIS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;

namespace CIS.Controllers
{
    // อนุญาตให้ทั้ง IT และ Admin ย่อย เข้าถึงได้
    [Authorize(Roles = @"CRIMCAD\CIS_Admins,CRIMCAD\HR_Users")]
#pragma warning disable CA1416
    public class AdminAdController : Controller
    {
        private readonly string _domainName = "CRIMCAD";
        private readonly string _ouRootPath = "OU=Crimc Users,DC=CRIMC,DC=INTRA";
        private readonly string _defaultPassword = "P@ssw0rd";

        // [HELPER] เช็คว่าเป็น "IT Super Admin" หรือไม่?
        private bool IsItSuperAdmin()
        {
            return User.IsInRole(@"CRIMCAD\CIS_Admins");
        }

        // ==========================================
        // 1. จัดการผู้ใช้ (USER MANAGEMENT)
        // ==========================================

        public IActionResult Index()
        {
            var users = new List<AdUserViewModel>();
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName, _ouRootPath))
                {
                    UserPrincipal userTemplate = new UserPrincipal(context);
                    PrincipalSearcher searcher = new PrincipalSearcher(userTemplate);
                    foreach (var result in searcher.FindAll())
                    {
                        if (result is UserPrincipal user)
                        {
                            users.Add(new AdUserViewModel
                            {
                                Username = user.SamAccountName ?? "N/A",
                                DisplayName = user.DisplayName ?? "N/A",
                                Email = user.EmailAddress ?? "N/A",
                                IsEnabled = user.Enabled ?? false,
                                IsLockedOut = user.IsAccountLockedOut()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"ค้นหา AD ไม่สำเร็จ: {ex.Message}";
            }
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            var model = new CreateUserViewModel { OUList = GetOUs(_ouRootPath) };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.OUList = GetOUs(_ouRootPath);
                return View(model);
            }
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName, model.SelectedOU))
                {
                    UserPrincipal user = new UserPrincipal(context)
                    {
                        SamAccountName = model.Username,
                        UserPrincipalName = $"{model.Username}@{_domainName.ToLower()}.intra",
                        DisplayName = $"{model.FirstName} {model.LastName}",
                        GivenName = model.FirstName,
                        Surname = model.LastName,
                        EmailAddress = model.Email,
                        Enabled = true
                    };
                    user.SetPassword(_defaultPassword);
                    user.ExpirePasswordNow();
                    user.Save();
                    using (var de = user.GetUnderlyingObject() as DirectoryEntry)
                    {
                        if (de != null)
                        {
                            de.Properties["employeeID"].Value = model.EmployeeID;
                            de.CommitChanges();
                        }
                    }
                }
                TempData["SuccessMessage"] = $"สร้างผู้ใช้ {model.Username} สำเร็จ! (รหัส: {_defaultPassword})";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                model.OUList = GetOUs(_ouRootPath);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult EditUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName))
                {
                    var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, id);
                    if (user == null) return NotFound();

                    string currentOuDn = "";
                    // ใช้ตัวแปรชั่วคราวเพื่ออ่านค่า OU โดยไม่ใช้ using(...) กับ user object
                    var de = user.GetUnderlyingObject() as DirectoryEntry;
                    if (de != null && de.Parent != null)
                    {
                        currentOuDn = de.Parent.Properties["distinguishedName"].Value.ToString() ?? "";
                    }

                    var model = new EditUserViewModel
                    {
                        Username = user.SamAccountName,
                        FirstName = user.GivenName,
                        LastName = user.Surname,
                        Email = user.EmailAddress,
                        IsEnabled = user.Enabled ?? false,
                        SelectedOU = currentOuDn
                    };

                    // *** เช็คสิทธิ์: โหลดข้อมูล Group/OU เฉพาะ IT Super Admin ***
                    if (IsItSuperAdmin())
                    {
                        model.OUList = GetOUs(_ouRootPath);
                        var allGroups = GetAllSecurityGroups(context);
                        var userGroupNames = user.GetGroups()
                                                 .Select(g => g.SamAccountName)
                                                 .ToHashSet();

                        model.GroupMemberships = allGroups.Select(group => new GroupCheckboxViewModel
                        {
                            SamAccountName = group.SamAccountName,
                            DisplayName = group.Name,
                            IsSelected = userGroupNames.Contains(group.SamAccountName)
                        })
                        .OrderBy(g => g.DisplayName)
                        .ToList();
                    }

                    ViewBag.IsSuperAdmin = IsItSuperAdmin();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"ค้นหา User ไม่สำเร็จ: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (IsItSuperAdmin()) model.OUList = GetOUs(_ouRootPath);
                ViewBag.IsSuperAdmin = IsItSuperAdmin();
                return View(model);
            }

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName))
                {
                    var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, model.Username);
                    if (user == null) return NotFound();

                    // 1. [ย้าย OU] (ทำเฉพาะ IT Super Admin)
                    if (IsItSuperAdmin())
                    {
                        var userEntry = user.GetUnderlyingObject() as DirectoryEntry;
                        if (userEntry != null)
                        {
                            string currentParentDn = userEntry.Parent.Properties["distinguishedName"].Value.ToString() ?? "";
                            if (!string.Equals(currentParentDn, model.SelectedOU, StringComparison.OrdinalIgnoreCase))
                            {
                                using (var targetOuEntry = new DirectoryEntry($"LDAP://{model.SelectedOU}"))
                                {
                                    userEntry.MoveTo(targetOuEntry);
                                }
                            }
                        }
                    }

                    // 2. [แก้ไข Properties] (ทำได้ทุกคน)
                    user.GivenName = model.FirstName;
                    user.Surname = model.LastName;
                    user.DisplayName = $"{model.FirstName} {model.LastName}";
                    user.EmailAddress = model.Email;
                    user.Enabled = model.IsEnabled;
                    user.Save();

                    // 3. [อัปเดต Group] (ทำเฉพาะ IT Super Admin)
                    if (IsItSuperAdmin() && model.GroupMemberships != null)
                    {
                        var currentGroupNames = user.GetGroups().Select(g => g.SamAccountName).ToHashSet();

                        foreach (var groupCheckbox in model.GroupMemberships)
                        {
                            bool isCurrentlyMember = currentGroupNames.Contains(groupCheckbox.SamAccountName);
                            var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupCheckbox.SamAccountName);
                            if (group == null) continue;

                            if (groupCheckbox.IsSelected && !isCurrentlyMember)
                            {
                                group.Members.Add(user);
                                group.Save();
                            }
                            else if (!groupCheckbox.IsSelected && isCurrentlyMember)
                            {
                                if (string.Equals(groupCheckbox.SamAccountName, "Domain Users", StringComparison.OrdinalIgnoreCase)) continue;
                                group.Members.Remove(user);
                                group.Save();
                            }
                        }
                    }
                }

                TempData["SuccessMessage"] = $"แก้ไขผู้ใช้ {model.Username} สำเร็จ!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                if (IsItSuperAdmin()) model.OUList = GetOUs(_ouRootPath);
                ViewBag.IsSuperAdmin = IsItSuperAdmin();
                return View(model);
            }
        }

        // ==========================================
        // 2. จัดการกลุ่ม (GROUP MANAGEMENT) - (เอากลับมาแล้ว!)
        // ==========================================

        public IActionResult GroupList()
        {
            var groups = new List<AdGroupViewModel>();
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName, _ouRootPath))
                {
                    GroupPrincipal groupTemplate = new GroupPrincipal(context);
                    PrincipalSearcher searcher = new PrincipalSearcher(groupTemplate);

                    foreach (var result in searcher.FindAll())
                    {
                        if (result is GroupPrincipal group)
                        {
                            groups.Add(new AdGroupViewModel
                            {
                                SamAccountName = group.SamAccountName,
                                Description = group.Description ?? "-",
                                DistinguishedName = group.DistinguishedName
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"โหลดข้อมูลไม่สำเร็จ: {ex.Message}";
            }
            return View(groups.OrderBy(g => g.SamAccountName).ToList());
        }

        [HttpGet]
        public IActionResult CreateGroup()
        {
            var model = new CreateUserViewModel { OUList = GetOUs(_ouRootPath) };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateGroup(CreateUserViewModel model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.SelectedOU))
            {
                ModelState.AddModelError("", "กรุณาระบุชื่อกลุ่มและเลือก OU");
                model.OUList = GetOUs(_ouRootPath);
                return View(model);
            }

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName, model.SelectedOU))
                {
                    GroupPrincipal newGroup = new GroupPrincipal(context)
                    {
                        SamAccountName = model.Username,
                        Name = model.Username,
                        Description = "Created by CIS Admin",
                        GroupScope = GroupScope.Global,
                        IsSecurityGroup = true
                    };
                    newGroup.Save();
                }
                TempData["SuccessMessage"] = $"สร้างกลุ่ม {model.Username} สำเร็จ!";
                return RedirectToAction("GroupList");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                model.OUList = GetOUs(_ouRootPath);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ManageGroup(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName))
                {
                    var group = GroupPrincipal.FindByIdentity(context, id);
                    if (group == null) return NotFound();

                    var model = new ManageGroupViewModel
                    {
                        GroupName = group.SamAccountName,
                        Description = group.Description
                    };

                    foreach (var principal in group.GetMembers())
                    {
                        if (principal is UserPrincipal user)
                        {
                            model.CurrentMembers.Add(new AdUserViewModel
                            {
                                Username = user.SamAccountName,
                                DisplayName = user.DisplayName,
                                Email = user.EmailAddress
                            });
                        }
                    }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("GroupList");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMemberToGroup(ManageGroupViewModel model)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName))
                {
                    var group = GroupPrincipal.FindByIdentity(context, model.GroupName);
                    var user = UserPrincipal.FindByIdentity(context, model.NewUsernameToAdd);

                    if (group == null) return NotFound("ไม่พบกลุ่ม");
                    if (user == null)
                    {
                        TempData["ErrorMessage"] = $"ไม่พบผู้ใช้: {model.NewUsernameToAdd}";
                        return RedirectToAction("ManageGroup", new { id = model.GroupName });
                    }

                    if (group.Members.Contains(user))
                    {
                        TempData["ErrorMessage"] = $"{user.SamAccountName} เป็นสมาชิกอยู่แล้ว";
                    }
                    else
                    {
                        group.Members.Add(user);
                        group.Save();
                        TempData["SuccessMessage"] = $"เพิ่ม {user.SamAccountName} เข้ากลุ่มแล้ว";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }
            return RedirectToAction("ManageGroup", new { id = model.GroupName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveMemberFromGroup(string groupName, string username)
        {
            try
            {
                if (groupName.Equals("Domain Users", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "ห้ามยุ่งกับ Domain Users!" });

                using (var context = new PrincipalContext(ContextType.Domain, _domainName))
                {
                    var group = GroupPrincipal.FindByIdentity(context, groupName);
                    var user = UserPrincipal.FindByIdentity(context, username);

                    if (group != null && user != null)
                    {
                        group.Members.Remove(user);
                        group.Save();
                        return Json(new { success = true, message = $"ลบ {username} ออกจากกลุ่มแล้ว" });
                    }
                    return Json(new { success = false, message = "ไม่พบข้อมูล" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // 3. API & HELPER METHODS
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(string id)
        {
            if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "ไม่พบ ID" });
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName))
                {
                    var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, id);
                    if (user == null) return Json(new { success = false, message = "ไม่พบผู้ใช้" });
                    user.SetPassword(_defaultPassword);
                    user.ExpirePasswordNow();
                    user.Save();
                }
                return Json(new { success = true, message = $"รีเซ็ตรหัสผ่านของ {id} สำเร็จ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "ไม่พบ ID" });
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName))
                {
                    var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, id);
                    if (user == null) return Json(new { success = false, message = "ไม่พบผู้ใช้" });
                    user.Delete();
                }
                return Json(new { success = true, message = $"ลบผู้ใช้ {id} สำเร็จ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnlockUser(string id)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName))
                {
                    var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, id);
                    if (user != null && user.IsAccountLockedOut())
                    {
                        user.UnlockAccount();
                        return Json(new { success = true, message = $"ปลดล็อก {id} สำเร็จ!" });
                    }
                    return Json(new { success = false, message = "ผู้ใช้ไม่ได้ถูกล็อก" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleUserState(string id)
        {
            if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "ไม่พบ ID" });
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName))
                {
                    var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, id);
                    if (user == null) return Json(new { success = false, message = "ไม่พบผู้ใช้" });

                    // ปรับปรุง: เช็คค่า Null
                    if (user.Enabled.HasValue) user.Enabled = !user.Enabled.Value;
                    else user.Enabled = true;

                    user.Save();
                    string actionText = (user.Enabled == true) ? "เปิดใช้งาน" : "ระงับ";
                    return Json(new { success = true, message = $"{actionText}บัญชี {id} สำเร็จ!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateOU(string ouName)
        {
            try
            {
                using (var parentEntry = new DirectoryEntry($"LDAP://{_ouRootPath}"))
                {
                    DirectoryEntry newOu = parentEntry.Children.Add($"OU={ouName}", "OrganizationalUnit");
                    newOu.Properties["description"].Value = "สร้างโดย CIS Web App";
                    newOu.CommitChanges();
                }
                TempData["SuccessMessage"] = $"สร้าง OU={ouName} ใน {_ouRootPath} สำเร็จ!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private List<SelectListItem> GetOUs(string rootPath)
        {
            var ouList = new List<SelectListItem>();
            try
            {
                using (var rootEntry = new DirectoryEntry($"LDAP://{rootPath}"))
                {
                    using (var searcher = new DirectorySearcher(rootEntry))
                    {
                        searcher.Filter = "(objectClass=organizationalUnit)";
                        searcher.SearchScope = SearchScope.OneLevel;
                        searcher.PropertiesToLoad.Add("name");
                        searcher.PropertiesToLoad.Add("distinguishedName");
                        ouList.Add(new SelectListItem
                        {
                            Text = $"(ราก) {rootEntry.Name.Replace("OU=", "")}",
                            Value = rootEntry.Properties["distinguishedName"].Value.ToString()
                        });
                        foreach (SearchResult result in searcher.FindAll())
                        {
                            ouList.Add(new SelectListItem
                            {
                                Text = result.Properties["name"][0].ToString(),
                                Value = result.Properties["distinguishedName"][0].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                if (!ouList.Any()) ouList.Add(new SelectListItem { Text = "Error: ไม่สามารถโหลด OU ได้", Value = "" });
            }
            return ouList;
        }

        private List<GroupPrincipal> GetAllSecurityGroups(PrincipalContext context)
        {
            var groups = new List<GroupPrincipal>();
            GroupPrincipal groupTemplate = new GroupPrincipal(context)
            {
                IsSecurityGroup = true,
                GroupScope = GroupScope.Global
            };
            PrincipalSearcher searcher = new PrincipalSearcher(groupTemplate);
            foreach (var result in searcher.FindAll())
            {
                if (result is GroupPrincipal group)
                {
                    if (group.Sid.IsWellKnown(WellKnownSidType.BuiltinUsersSid) ||
                        group.Sid.IsWellKnown(WellKnownSidType.AccountDomainGuestsSid) ||
                        string.Equals(group.SamAccountName, "Domain Admins", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    groups.Add(group);
                }
            }
            return groups;
        }
    }
}