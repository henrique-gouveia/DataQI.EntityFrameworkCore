using System;
using Bogus;

namespace DataQI.EntityFrameworkCore.Test.Repository.Products
{
    public class ProductBuilder
    {
        private readonly Product product;

        private ProductBuilder()
        {
            var faker = new Faker();
            var price = Math.Round(faker.Random.Decimal(max: 1000), 2);

            product = new Product()
            {
                Active = faker.Random.Bool(),
                Ean = faker.Random.ULong(max: 999999999999).ToString(),
                Reference = faker.Random.ULong(max: 999999999999).ToString(),
                Name = faker.Commerce.ProductName(),
                Department = faker.Commerce.Department(),
                Price = price,
                ListPrice = price * 100,
                Keywords = $"{faker.Commerce.ProductAdjective()}, {faker.Commerce.ProductAdjective()}, {faker.Commerce.ProductAdjective()}",
                Stock = faker.Random.Short(),
                DateRegister = faker.Date.Past(2)
            };
        }

        public static ProductBuilder NewInstance() => new ProductBuilder();

        public ProductBuilder SetId(int id)
        {
            product.Id = id;
            return this;
        }

        public ProductBuilder SetActive(bool active)
        {
            product.Active = active;
            return this;
        }

        public ProductBuilder SetEan(string ean)
        {
            product.Ean = ean;
            return this;
        }

        public ProductBuilder SetReference(string reference)
        {
            product.Reference = reference;
            return this;
        }

        public ProductBuilder SetName(string name)
        {
            product.Name = name;
            return this;
        }

        public ProductBuilder SetDepartament(string department)
        {
            product.Department = department;
            return this;
        }

        public ProductBuilder SetPrice(decimal price)
        {
            product.Price = price;
            return this;
        }

        public ProductBuilder SetListPrice(decimal listPrice)
        {
            product.ListPrice = listPrice;
            return this;
        }

        public ProductBuilder SetKeywords(string keywords)
        {
            product.Keywords = keywords;
            return this;
        }

        public ProductBuilder SetDateRegister(DateTime dateRegister)
        {
            product.DateRegister = dateRegister;
            return this;
        }

        public Product Build() => product;
    }
}