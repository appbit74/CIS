using System;
using System.Collections.Generic;
using System.Linq; // จำเป็นสำหรับการใช้ OrderBy
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CIS.Services
{
    public class CrimsApiService : ICrimsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public CrimsApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // อ่าน URL หลักจาก Config (ควรเป็น http://crims.web/crimsExternalApi/API/EXT/getData)
            _apiUrl = configuration["ExternalApi:CrimsUrl"];
        }

        // 1. ดึงหน่วยงาน (Department)
        public async Task<Dictionary<string, string>> GetSectionsAsync()
        {
            // ส่งค่าตามสเปคใหม่: table_name, field_id, field_name
            return await FetchDataFromApi("pdepartment", "dep_code", "dep_name");
        }

        // 2. ดึงตำแหน่ง (Position)
        public async Task<Dictionary<string, string>> GetPositionsAsync()
        {
            // ส่งค่าตามสเปคใหม่: table_name, field_id, field_name
            // หมายเหตุ: เช็คชื่อตารางดีๆ นะครับ ในโจทย์คุณให้มาเป็น "pposition" (ไม่มี s)
            return await FetchDataFromApi("pposition", "post_id", "post_name");
        }

        // --- Private Helper Method (ปรับปรุงใหม่) ---
        private async Task<Dictionary<string, string>> FetchDataFromApi(string tableName, string fieldId, string fieldName)
        {
            var emptyResult = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(_apiUrl)) return emptyResult;

            try
            {
                // สร้าง Payload ให้ตรงกับโครงสร้างใหม่
                var payload = new
                {
                    table_name = tableName,
                    field_id = fieldId,
                    field_name = fieldName
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // ยิงไปที่ URL เดิม (getData)
                var response = await _httpClient.PostAsync(_apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();

                    // แปลง JSON ที่ได้กลับมาเป็น Dictionary<string, string>
                    // (สมมติว่า API ตอบกลับมาเป็น Format {"1": "ชื่อ", "2": "ชื่อ"} เหมือนเดิม)
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

                    // ✅ ปรับปรุง: เรียงลำดับตามชื่อ (Value) ก-ฮ
                    if (data != null)
                    {
                        return data.OrderBy(x => x.Value)
                                   .ToDictionary(x => x.Key, x => x.Value);
                    }

                    return emptyResult;
                }
                else
                {
                    Console.WriteLine($"API Error ({tableName}): {response.StatusCode}");
                    return emptyResult;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception calling Crims API ({tableName}): {ex.Message}");
                return emptyResult;
            }
        }
    }
}