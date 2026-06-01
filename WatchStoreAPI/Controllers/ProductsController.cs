using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreAPI.Data;
using WatchStoreAPI.Models;

namespace WatchStoreAPI.Controllers
{

   [Route("api/[controller]")]
   [ApiController]
   public class ProductsController : ControllerBase
   {
      private ApiDbContext dbContext;
      public ProductsController(ApiDbContext dbContext)
      {
         this.dbContext = dbContext;
      }


      [HttpGet]
      public async Task<ActionResult> Get([FromQuery]string search,[FromQuery]int? categoryId,[FromQuery] string material,[FromQuery] string gender,[FromQuery]decimal? minPrice,[FromQuery]decimal? maxPrice,[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
      {

         var query= dbContext.Products.AsQueryable();
         if (!string.IsNullOrEmpty(search))
         {
            query=query.Where(p => p.Name.ToLower().Contains(search.ToLower())||p.Description.ToLower().Contains(search.ToLower()));
         }
         if (!string.IsNullOrEmpty(material))
         {
            query = query.Where(p => p.Material.ToLower() == material.ToLower());
         }
         if (!string.IsNullOrEmpty(gender))
         {
            query=query.Where(p => p.Gender.ToLower() == gender.ToLower());
         }
         if (minPrice.HasValue)
            {
            query=query.Where(p => p.Price >= minPrice.Value);
            }
         if (maxPrice.HasValue)
         {
            query = query.Where(p => p.Price <= maxPrice.Value);
            }
         if (categoryId.HasValue)
         {
            query = query.Where(p => p.CategoryId == categoryId);
         }
        var products= await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(products);
      }
      [HttpGet("{id}")]
      public async Task<ActionResult> Get(int id)
      {
         var product = (await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id));
         if (product == null)
         {
            return NotFound();
         }
         return Ok(product);
      }
      [Authorize(Roles ="Admin")]
      [HttpPost]
      public async Task<ActionResult> Post([FromForm] Product product)
      {
         var guid = Guid.NewGuid();
        var filePath= Path.Combine("wwwroot", guid + ".jpg");
         using (var fileStream=new FileStream(filePath,FileMode.Create)) {
            await product.Image.CopyToAsync(fileStream);
        }
         product.ImageUrl = filePath;
         if (product == null)
         {
            return BadRequest("Product is null");
         }
         await dbContext.Products.AddAsync(product);
         await dbContext.SaveChangesAsync();
         return StatusCode(StatusCodes.Status201Created);
      }
      [Authorize(Roles ="Admin")]
      [HttpPut("{id}")]
      public async Task<ActionResult> Put(int id, [FromForm] Product product)
      {
         var exisitingProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
         if (exisitingProduct != null)
         {
            exisitingProduct.Name = product.Name;
            exisitingProduct.Description = product.Description;
            exisitingProduct.Price = product.Price;
            exisitingProduct.CategoryId = product.CategoryId;
            if (product.Image != null)
            {
               if (string.IsNullOrEmpty(exisitingProduct.ImageUrl))
               {
                  var oldImagePath = Path.Combine("wwwroot", exisitingProduct.ImageUrl);
                  if (System.IO.File.Exists(oldImagePath))
                  {
                     System.IO.File.Delete(oldImagePath);
                  }
               }
               var guid = Guid.NewGuid();
        var filePath= Path.Combine("wwwroot", guid + ".jpg");
         using (var fileStream=new FileStream(filePath,FileMode.Create)) {
            await product.Image.CopyToAsync(fileStream);
        }
         exisitingProduct.ImageUrl = filePath;
            }
            
            await dbContext.SaveChangesAsync();
            return Ok("Record Updated....");
         }
         return NotFound();
        
      }
      [Authorize(Roles ="Admin")]
      [HttpDelete("{id}")]
      public async Task<ActionResult> Delete(int id)
      {
         var exisitingProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
         if (exisitingProduct != null)
         {
            if (string.IsNullOrEmpty(exisitingProduct.ImageUrl))
               {
                  var oldImagePath = Path.Combine("wwwroot", exisitingProduct.ImageUrl);
                  if (System.IO.File.Exists(oldImagePath))
                  {
                     System.IO.File.Delete(oldImagePath);
                  }
               }
            dbContext.Products.Remove(exisitingProduct);
            await dbContext.SaveChangesAsync();
            return Ok("Record has been Deleted");
         }
         return NotFound();
         
      }
   }
}