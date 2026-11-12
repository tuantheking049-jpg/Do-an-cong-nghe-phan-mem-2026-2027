using Microsoft.AspNetCore.Mvc;
using System.Linq; // Thư viện để dùng lệnh .Sum() cộng tổng

namespace VisitorGuide.Controllers
{
    [Route("api/thongke")]
    [ApiController]
    public class ThongKeController : ControllerBase
    {
        private static int _soLuotTruyCap = 0;
        
        // Mảng lưu số tĩnh ban đầu (Tổng = 178)
        // [0]Tân Huê Viên: 32, [1]Chợ Ngã Năm: 31, [2]Chùa Phật Học 2: 35, 
        // [3]Chùa La Hán: 20, [4]Căn cứ tỉnh ủy: 30, [5]Chùa Dơi: 30
        private static int[] _luotNgheCacDiem = new int[] { 32, 31, 35, 20, 30, 30 };

        [HttpGet]
        public IActionResult GetThongKe()
        {
            return Ok(new
            {
                luotTruyCap = _soLuotTruyCap,
                luotNghe = _luotNgheCacDiem.Sum(), // Tự động lấy 6 số cộng lại
                chiTietNghe = _luotNgheCacDiem     // Gửi luôn mảng 6 số xuống cho Admin
            });
        }

        [HttpPost("tang-truy-cap")]
        public IActionResult TangTruyCap()
        {
            _soLuotTruyCap++;
            return Ok(new { luotTruyCap = _soLuotTruyCap });
        }

        [HttpPost("giam-truy-cap")]
        public IActionResult GiamTruyCap()
        {
            if (_soLuotTruyCap > 0) _soLuotTruyCap--;
            return Ok();
        }

        // BẮT ID TỪ WEB USER ĐỂ CỘNG ĐÚNG CHỖ
        [HttpPost("tang-nghe/{id}")]
        public IActionResult TangNghe(string id)
        {
            string lowerId = id.ToLower();
            if (lowerId.Contains("nga-nam")) _luotNgheCacDiem[0]++;
            else if (lowerId.Contains("doi")) _luotNgheCacDiem[1]++;
            else if (lowerId.Contains("la-han")) _luotNgheCacDiem[2]++;
            else if (lowerId.Contains("phat-hoc")) _luotNgheCacDiem[3]++;
            else if (lowerId.Contains("can-cu")) _luotNgheCacDiem[4]++;
            else if (lowerId.Contains("tan-hue")) _luotNgheCacDiem[5]++;
            
            return Ok();
        }
    }
}