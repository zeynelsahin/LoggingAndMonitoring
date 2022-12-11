using CarvedRock.Data;
using CarvedRock.Data.Entities;
using CarvedRock.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CarvedRock.Domain;

public class ProductLogic : IProductLogic
{
    private readonly ILogger<ProductLogic> _logger;
    private readonly ICarvedRockRepository _repository;

    public ProductLogic(ILogger<ProductLogic> logger, ICarvedRockRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task<IEnumerable<Product>> GetProductsForCategoryAsync(string category)
    {
        _logger.LogInformation("Getting products in logic for {category}", category);

        return await _repository.GetProductsAsync(category);
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _repository.GetProductByIdAsync(id);
    }

    public IEnumerable<Product> GetProductsForCategory(string category)
    {
        return _repository.GetProducts(category);
    }

    public Product? GetProductById(int id)
    {
        _logger.LogDebug("Logic for single product ({id})", id);
        return _repository.GetProductById(id);
    }
}