using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIS.Models
{
    // 1. สถานะใบเบิก
    public enum SupplyStatus
    {
        [Display(Name = "รออนุมัติ")]
        Pending = 0,

        [Display(Name = "อนุมัติแล้ว/รอรับของ")]
        Approved = 1,

        [Display(Name = "รับของแล้ว (เสร็จสิ้น)")]
        Completed = 2,

        [Display(Name = "ไม่อนุมัติ")]
        Rejected = 9,

        [Display(Name = "ยกเลิก")]
        Cancelled = -1
    }

    // 2. รายการวัสดุ (สินค้า)
    public class SupplyItem
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "ระบุชื่อวัสดุ")]
        [Display(Name = "ชื่อวัสดุ")]
        public string ItemName { get; set; } // เช่น ปากกาลูกลื่นสีน้ำเงิน

        [Display(Name = "รายละเอียด/รุ่น")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "หน่วยนับ")]
        public string Unit { get; set; } = "ชิ้น"; // ด้าม, กล่อง, แผ่น

        [Display(Name = "รูปภาพ")]
        public string? ImagePath { get; set; } // เก็บ Path รูป

        [Display(Name = "จำนวนคงเหลือ")]
        public int StockQuantity { get; set; } = 0;

        [Display(Name = "จุดแจ้งเตือนเมื่อของใกล้หมด")]
        public int LowStockThreshold { get; set; } = 5;

        public bool IsActive { get; set; } = true; // เปิด/ปิด การเบิก
    }

    // 3. ใบเบิก (Header)
    public class SupplyOrder
    {
        [Key]
        public int Id { get; set; }

        // ผู้เบิก (เชื่อมกับ EmployeeProfile)
        [Display(Name = "ผู้เบิก")]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual EmployeeProfile? Requester { get; set; }

        [Display(Name = "วันที่เบิก")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Display(Name = "สถานะ")]
        public SupplyStatus Status { get; set; } = SupplyStatus.Pending;

        [Display(Name = "หมายเหตุการเบิก")]
        public string? UserRemark { get; set; } // เช่น ขอเบิกไปใช้ในโครงการ...

        // ส่วนของ Admin
        [Display(Name = "ผู้อนุมัติ")]
        public string? ApprovedBy { get; set; } // เก็บ Username คนกดอนุมัติ

        [Display(Name = "วันที่อนุมัติ")]
        public DateTime? ApprovedDate { get; set; }

        [Display(Name = "เหตุผล(กรณีไม่อนุมัติ)")]
        public string? AdminRemark { get; set; }

        // รายการในบิล
        public virtual ICollection<SupplyOrderItem> OrderItems { get; set; } = new List<SupplyOrderItem>();
    }

    // 4. รายละเอียดใบเบิก (Detail)
    public class SupplyOrderItem
    {
        [Key]
        public int Id { get; set; }

        public int SupplyOrderId { get; set; }
        [ForeignKey("SupplyOrderId")]
        public virtual SupplyOrder? Order { get; set; }

        public int SupplyItemId { get; set; }
        [ForeignKey("SupplyItemId")]
        public virtual SupplyItem? Item { get; set; }

        [Display(Name = "จำนวนที่เบิก")]
        [Range(1, 100, ErrorMessage = "เบิกได้อย่างน้อย 1 ชิ้น")]
        public int Quantity { get; set; }
    }
}