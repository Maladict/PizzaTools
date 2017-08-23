using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InMemTests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pizzeria.Controllers;
using Pizzeria.Data;
using Pizzeria.Models;
using Xunit;

namespace PizzeriaUnitTests
{
    public class DishControllerTests
    {
        //[Fact]
        //public void PassingTest()
        //{
        //    Assert.Equal(4, 2+2);
        //}

        [Fact]
        public async Task Test1()
        {
            //Arrange         
            var fakeDishes = new List<Dish>()
            {
                new Dish(){DishId = 1, Name = "Hawaii"},
                new Dish(){DishId = 2, Name = "Vezuvio"},
                new Dish(){DishId = 3, Name = "Vegetariana"},
            }.ToAsyncDbSetMock();

            var mockDbContext = new Mock<ApplicationDbContext>();
            mockDbContext.Setup(repo => repo.Dishes).Returns(fakeDishes.Object);
            var controller= new DishController(mockDbContext.Object);

            //Act
            var result = await controller.Index();

            //Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Dish>>(
                viewResult.ViewData.Model);
            Assert.Equal(3, model.Count());
        }
    }
}