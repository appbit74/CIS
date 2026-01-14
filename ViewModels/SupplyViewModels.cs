using System.Collections.Generic;

namespace CIS.ViewModels
{
    // ใช้สำหรับรับข้อมูล JSON จากหน้าบ้านตอนกดสั่งซื้อ
    public class OrderRequestViewModel
    {
        public string Remark { get; set; }
        public List<OrderItemRequest> Items { get; set; }
    }

    public class OrderItemRequest
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }
}