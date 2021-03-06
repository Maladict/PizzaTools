﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pizzeria.Extensions;
using Pizzeria.Models;

namespace Pizzeria.Commands
{
    public class GetTotalHomeControllerCommand : BaseHomeControllerCommand
    {
        public override async Task<IActionResult> Execute(object id, IFormCollection formCollection)
        {
            var context = this.Context;

            //Handle quantity updates
            var keys = formCollection["item.Quantity"];
            var basket = context.Baskets
                .Include(y => y.Items)
                .ThenInclude(f => f.BasketItemIngredients)
                .FirstOrDefault(x => x.BasketId == Convert.ToInt32(id));

            for (var i = 0; i < basket.Items.Count; i++)
            {
                basket.Items[i].Quantity = Convert.ToInt32(keys[i]);
            }
            await context.SaveChangesAsync();

            return Controller.RedirectToAction("Index", "Home");
        }
    }
}
