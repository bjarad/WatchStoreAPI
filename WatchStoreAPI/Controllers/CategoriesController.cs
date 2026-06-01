using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreAPI.Data;
using WatchStoreAPI.Models;

namespace WatchStoreAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private ApiDbContext dbContext;
        public CategoriesController(ApiDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            var categories = await dbContext.Categories.ToListAsync();
            return Ok(categories);
        }
        [Authorize(Roles ="Admin")]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] Category category)
        {
            if (category == null)
            {
                return BadRequest("Category is null");
            }
            await dbContext.Categories.AddAsync(category);
            await dbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);

        }
        [Authorize(Roles ="Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromBody] Category category)
        {
            var exisitingcategory = await dbContext.Categories.FindAsync(id);
            if (exisitingcategory == null)
            {
                return NotFound();
            }
            exisitingcategory.Name = category.Name;
            await dbContext.SaveChangesAsync();
            return Ok("Category Updated....");
        }
        [Authorize(Roles ="Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var exisitingcategory = await dbContext.Categories.FindAsync(id);
            if (exisitingcategory == null)
            {
                return NotFound();
            }
            dbContext.Categories.Remove(exisitingcategory);
            await dbContext.SaveChangesAsync();
            return Ok("Record has been deleted....");
        }
   

    }
}