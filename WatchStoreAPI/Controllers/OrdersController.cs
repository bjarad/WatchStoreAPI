using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreAPI.Data;
using WatchStoreAPI.Models;

namespace WatchStoreAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private ApiDbContext dbContext;
        public OrdersController(ApiDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        [Authorize(Roles ="Admin")]
        [HttpGet("admin")]
        public async Task <IActionResult> GetAllOrdersForAdmin(int pageNumber=1,int pageSize=10
                                                               ,[FromQuery] string?status=null
                                                               ,[FromQuery]DateTime?startDate=null
                                                               ,[FromQuery]DateTime?endDate=null
                                                               ,[FromQuery]string?user=null){
         var query=dbContext.Orders.AsQueryable();
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);

         }
            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate);
         }
            if (endDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= endDate);
         }
            if (!string.IsNullOrEmpty(user))
            {
                query = query.Where(o => o.User.Name.Contains(user) || o.User.Email.Contains(user));
         }
         var orders=await query.OrderByDescending(o=>o.OrderDate).Skip((pageNumber-1)*pageSize).Take(pageSize)
                                                       .Select(o=>new{
                                                        Id=o.Id,
                                                        UserName=o.User.Name,
                                                        OrderDate=o.OrderDate,
                                                        TotalAmount=o.TotalAmount,
                                                        Status=o.Status,
                                                        Address=o.Address
                                                       }).ToListAsync();
          return Ok(orders);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/pending")]
        public async Task<IActionResult> GetPendingOredersForAdmin(int pageNumber = 1, int pageSize = 10)
        {
            var pendingOrders = await dbContext.Orders.Where(o => o.Status.ToLower() == "pending")
                                                           .OrderByDescending(o => o.OrderDate)
                                                           .Skip((pageNumber - 1) * pageSize)
                                                           .Take(pageSize)
                                                           .Select(o=>new{
                                                        Id=o.Id,
                                                        UserName=o.User.Name,
                                                        OrderDate=o.OrderDate,
                                                        TotalAmount=o.TotalAmount,
                                                        Status=o.Status,
                                                        Address=o.Address
                                                       })
                                                           .ToListAsync();
            return Ok(pendingOrders);
        }
        
        [HttpPut("{orderId:int}/status")]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId,[FromQuery]string orderStatus){
       if(orderStatus!="completed" && orderStatus!="cancelled"){
        return BadRequest("Invalid Status .Allows values completed or cancelled");
       }
       var order=await dbContext.Orders.FindAsync(orderId);
       if(order==null){
        return NotFound("Order not found");
       }
       order.Status=orderStatus;
       await dbContext.SaveChangesAsync();
       return Ok("Status Updated ");
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("{orderId:int}/admindetails")]
        public async Task<IActionResult> GetOrderDetailsForAdmin(int orderId)
        {
            var orderDetails = await dbContext.OrderDetails.Where(od => od.OrderId == orderId)
                                         .Include(od => od.Product)
                                         .Select(od => new
                                         {
                                             Id = od.Id,
                                             Qty = od.Qty,
                                             TotalAmount = od.TotalAmount,
                                             ProductId = od.ProductId,
                                             productName = od.Product.Name,
                                             productImageUrl = od.Product.ImageUrl,
                                             productPrice = od.Product.Price

                                         }).ToListAsync();
            return Ok(orderDetails);
        }
        [HttpGet("my")]
        public async Task<IActionResult> GetOrdersForCurrentUser()
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return Unauthorized();
            }
            var userOrders= await dbContext.Orders.Where(o => o.UserId == user.Id).OrderByDescending(o => o.OrderDate)
                                                                      .Select(o=>new
                                                                      {
                                                                          o.Id,
                                                                          o.TotalAmount,
                                                                        o.OrderDate
                                                                      }).ToListAsync();
            return Ok(userOrders);
        }
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            await dbContext.Orders.AddAsync(order);
            await dbContext.SaveChangesAsync();
            var cartitems = await dbContext.ShoppingCartItems.Where(s => s.UserId == order.UserId).ToListAsync();
            order.OrderDate = DateTime.UtcNow;
            order.Status = "Pending";
            order.TotalAmount = cartitems.Sum(c => c.TotalAmount);
            foreach (var cartitem in cartitems)
            {
                var OrderDetail = new OrderDetail
                {
                    UnitPrice = cartitem.UnitPrice,
                    Qty = cartitem.Qty,
                    TotalAmount = cartitem.TotalAmount,
                    ProductId = cartitem.ProductId,
                    OrderId = order.Id

                };
                await dbContext.OrderDetails.AddAsync(OrderDetail);
            }
            await dbContext.SaveChangesAsync();
            dbContext.ShoppingCartItems.RemoveRange(cartitems);
            await dbContext.SaveChangesAsync();
            return Ok("Your order has been placed. Your order Id is" + order.Id);
        }
        [HttpGet("{OrderId:int}/details")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {

            var orderDetail = await dbContext.OrderDetails.Where(o => o.OrderId == orderId)
                                                        .Select(o => new
                                                        {
                                                            Id = o.Id,
                                                            Qty = o.Qty,
                                                            TotalAmount = o.TotalAmount,
                                                            ProductName = o.Product.Name,
                                                            ProductImageurl = o.Product.ImageUrl,
                                                            productPrice = o.Product.Price
                                                        }).ToListAsync();
            if (orderDetail == null)
            {
                return NotFound();
            }
            return Ok(orderDetail);
        }

    }
}