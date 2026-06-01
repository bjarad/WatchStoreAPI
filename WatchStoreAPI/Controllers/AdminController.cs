using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreAPI.Data;

namespace WatchStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private ApiDbContext dbContext;

        public AdminController(ApiDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        [HttpGet("dashboard")]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> GetAdminDashboardSummery()
        {
            var totatOrders = await dbContext.Orders.CountAsync();
            var pendingOrders = await dbContext.Orders.CountAsync(o => o.Status == "Pending");
            var totalRevenue = await dbContext.Orders.Where(o => o.Status == "completed").SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var totalProducts = await dbContext.Products.CountAsync();
            var totalCategories = await dbContext.Categories.CountAsync();
            var result = new
            {
                TotalOrders = totatOrders,
                PendingOrders = pendingOrders,
                TotalRevenue = totalRevenue,
                TotalProducts = totalProducts,
                totalCategories = totalCategories
            };
            return Ok(result);
        }
        [HttpGet("revenue")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRevnue([FromQuery] string range = "monthly")
        {
            DateTime now = DateTime.UtcNow;
            var result = new List<object>();
            for (var i = 6; i >= 0; i--)
            {
                DateTime start, end;
                string period;
                if (range == "yearly")
                {
                    var year = now.Year - i;
                    start = new DateTime(year, 1, 1);
                    end = start.AddYears(1);
                    period = year.ToString();
                }
                else if (range == "monthly")
                {
                    var date = now.AddMonths(-i);
                    start = new DateTime(date.Year, date.Month, 1);
                    end = start.AddMonths(1);
                    period = $"{date.Year}-{date.Month:02}";
                }
                else if (range == "weekly")
                {
                    var weekStart = now.Date.AddDays(-7 * i);
                    start = weekStart.AddDays(-(int)weekStart.DayOfWeek);
                    end = start.AddDays(7);
                    period = start.ToString("yyyy-MM-dd");
                }
                else
                {
                    return BadRequest("use range=Yearly, monthly,weekly");
                }
                decimal revenue = await dbContext.Orders.Where(o => o.Status == "completed" && o.OrderDate >= start && o.OrderDate <= end)
                                              .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                result.Add(new { Revenue = revenue, Period = period });
            }
            return Ok(result);
        }

    }
}