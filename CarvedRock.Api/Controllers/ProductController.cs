using CarvedRock.Data.Entities;
using CarvedRock.Domain;
using CarvedRock.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRock.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductLogic _productLogic;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IProductLogic productLogic, ILogger<ProductController> logger)
    {
        _productLogic = productLogic;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IEnumerable<Product>> Get(string category = "all")
    {
        _logger.LogInformation("Getting products in API for {category}",category);
        return await _productLogic.GetProductsForCategoryAsync(category);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Product),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute]int id)
    {
        // var product = await _productLogic.GetProductByIdAsync(id);
        _logger.LogDebug("Getting single product in API for {id}",id);
        var product = _productLogic.GetProductById(id);
        if (product!=null)
        {
            return Ok(product);
        }
        _logger.LogWarning("No product found for ID: {id}",id);
        return NotFound();
    }
    
}