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
    public class ShoppingCartItemsController : ControllerBase
    {
        private ApiDbContext dbContext;
        public ShoppingCartItemsController(ApiDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return Unauthorized();
            }
            var cartItems = await dbContext.ShoppingCartItems.Where(s => s.UserId == user.Id)
                                                                    .Include(s => s.Product)
                                                                    .Select(s => new
                                                                    {
                                                                        Id = s.Id,
                                                                        Qty = s.Qty,
                                                                        UnitPrice = s.UnitPrice,
                                                                        TotalAmount = s.TotalAmount,
                                                                        ProductId = s.ProductId,
                                                                        ProductName = s.Product.Name,
                                                                        ImageUrl = s.Product.ImageUrl,

                                                                    }
                                                                    ).ToListAsync();
            return Ok(cartItems);
        }
        [HttpPost("add")]
        public async Task<IActionResult> Post([FromBody] ShoppingCartItem shoppingCartItem)
        {
            var exisitingCartItem = await dbContext.ShoppingCartItems.FirstOrDefaultAsync(s => s.ProductId == shoppingCartItem.ProductId && s.UserId == shoppingCartItem.UserId);
            if (exisitingCartItem != null)
            {
                exisitingCartItem.Qty += shoppingCartItem.Qty;
                exisitingCartItem.TotalAmount = exisitingCartItem.UnitPrice * exisitingCartItem.Qty;
            }
            else
            {
                var productRecord = await dbContext.Products.FindAsync(shoppingCartItem.ProductId);
                var newCartItem = new ShoppingCartItem
                {
                    UserId = shoppingCartItem.UserId,

                    ProductId = shoppingCartItem.ProductId,
                    Qty = shoppingCartItem.Qty,
                    UnitPrice = productRecord.Price,
                    TotalAmount = productRecord.Price * shoppingCartItem.Qty,

                };
                await dbContext.ShoppingCartItems.AddAsync(newCartItem);
            }
            dbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }
        [HttpPut]
        public async Task<IActionResult> Put([FromQuery] int productId, [FromQuery] string action)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return Unauthorized();
            }
            var cartItem = await dbContext.ShoppingCartItems.FirstOrDefaultAsync(s => s.ProductId == productId && s.UserId == user.Id);
            if (cartItem == null)
            {
                return NotFound("Product not found in the cart.");
            }
            switch (action.ToLower())
            {
                case "increase":
                    cartItem.Qty += 1;
                    break;
                case "decrease":
                    if (cartItem.Qty > 1)
                        cartItem.Qty -= 1;
                    else
                    {
                        dbContext.ShoppingCartItems.Remove(cartItem);
                    }
                    break;
                default:
                    return BadRequest("Invalid action Use increase or decrease");

            }
            cartItem.TotalAmount = cartItem.UnitPrice * cartItem.Qty;
            dbContext.SaveChangesAsync();
            return Ok("shopping cart updated");
        }
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> Delete(int productId)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return Unauthorized();
            }
            var cartItem = await dbContext.ShoppingCartItems.FirstOrDefaultAsync(s => s.ProductId == productId && s.Id == user.Id);
            if (cartItem == null)
            {
                return NotFound();
            }
            dbContext.ShoppingCartItems.Remove(cartItem);
            dbContext.SaveChangesAsync();
            return Ok("record Deleted....");
        }
       
    }
}