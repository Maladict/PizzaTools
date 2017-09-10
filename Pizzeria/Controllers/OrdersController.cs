﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pizzeria.Commands;
using Pizzeria.Data;
using Pizzeria.Extensions;
using Pizzeria.Models;
using Pizzeria.Services;

namespace Pizzeria.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrdersController> _logger;
        private readonly IEmailSender _emailSender;

        public OrdersController(ApplicationDbContext context, ILogger<OrdersController> logger, IEmailSender emailSender)
        {
            _context = context;
            _logger = logger;
            _emailSender = emailSender;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Order.Include(o => o.Basket);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.Basket)
                .SingleOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            ViewData["BasketId"] = new SelectList(_context.Baskets, "BasketId", "BasketId");
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,Paid,Delivered,Finished,OrderDate,ApplicationUserId,BasketId")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BasketId"] = new SelectList(_context.Baskets, "BasketId", "BasketId", order.Basket.BasketId);
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order.SingleOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["BasketId"] = new SelectList(_context.Baskets, "BasketId", "BasketId", order.Basket.BasketId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,Paid,Delivered,Finished,OrderDate,ApplicationUserId,BasketId")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BasketId"] = new SelectList(_context.Baskets, "BasketId", "BasketId", order.Basket.BasketId);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.Basket)
                .SingleOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Order.SingleOrDefaultAsync(m => m.OrderId == id);
            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.OrderId == id);
        }


        public IActionResult Payment(CheckoutInfo checkoutInfo)
        {
            //var checkoutInfo = _context.CheckoutInfo;

            var order = _context.Order
                .Include(o => o.Basket)
                .FirstOrDefault(x => x.OrderId == checkoutInfo.OrderId);

            var basket = _context.Baskets
                .Include(b => b.Items)
                .ThenInclude(c => c.Dish)
                .FirstOrDefault(x => x.BasketId == order.Basket.BasketId);

            //Calculate total
            var total = 0;

            foreach (var basketItem in basket.Items)
            {
                total += (basketItem.Quantity * basketItem.Dish.Price);
            }

            var shipping = 0;

            if (total < 500)
            {
                shipping = 49;
            }
            
            total += shipping;

            order.Total = total;
            order.Shipping = shipping;
            _context.SaveChanges();

            return View(checkoutInfo);
        }

        public async Task<IActionResult> Confirmation()
        {
            var basketId = 0;

            var sessionId = HttpContext.Session.GetInt32("BasketId");
            if (sessionId != null)
            {
                basketId = sessionId.Value;
            }

            var order = _context.Order
                .FirstOrDefault(x => x.BasketId == basketId);

            var checkoutInfo = _context.CheckoutInfo.FirstOrDefault(x => x.OrderId == order.OrderId);

            //Send message to baker
            var bakerMessage = $"You have a new order with order id {order.OrderId}. <br><br>To view your new order, please go to www.pizzatools.com/orders <br><br>Happy baking!";
            await _emailSender.SendEmailAsync("asdf@gmail.com", "New order!", bakerMessage);

            //Send message to customer
            var customerMessage = $"Thank you for ordering from PizzaTools! Your item will arrive shortly. <br><br>Your order Id is {order.OrderId}.<br><br>Thank you!";
            await _emailSender.SendEmailAsync(checkoutInfo.Email, "Thank you for your order!", customerMessage);

            HttpContext.Session.Clear();
            
            return View(order.OrderId);
        }

        public async Task<IActionResult> LoginOrAnonymous()
        {

            ViewData["ReturnUrl"] = HttpContext.Request.GetUri().AbsolutePath;

            var basketId = 0;

            if (HttpContext.Session.GetInt32("BasketId") != null)
            {
                basketId = HttpContext.Session.GetInt32("BasketId").Value;
            }

            var basket = _context.Baskets
                .Include(x => x.Items)
                .ThenInclude(y => y.BasketItemIngredients)
                .Include(z => z.Items)
                .ThenInclude(h => h.Dish)
                .ThenInclude(j => j.DishIngredients)
                .ThenInclude(k => k.Ingredient)
                .FirstOrDefault(x => x.BasketId == basketId);

            var order = new Order
            {
                Basket = basket
            };

            if (User.Identity.IsAuthenticated)
            {
                if (HttpContext.Session.GetInt32("LoggedInBefore") != null)
                {
                    order = _context.Order
                        .Include(x => x.Basket)
                        .ThenInclude(y => y.Items)
                        .ToList().Last();
                }
                
                var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

                var user = _context.Users.FirstOrDefault(x => x.Id == userId);

                order.User = user;
                _context.AddOrUpdate(order);
               
                await _context.SaveChangesAsync();

                var chOutInfo = _context.CheckoutInfo.Last();

                chOutInfo.FirstName = user.FirstName;
                chOutInfo.LastName = user.LastName;
                chOutInfo.PostingAddress = user.PostingAddress;
                chOutInfo.PostalCode = user.PostalCode;
                chOutInfo.City = user.City;
                chOutInfo.Email = user.Email;
                chOutInfo.PhoneNumber = Convert.ToInt32(user.PhoneNumber);
                
                _context.AddOrUpdate(chOutInfo);
                _context.AddOrUpdate(order);
                order.BasketId = basketId;

                await _context.SaveChangesAsync();

                HttpContext.Session.Remove("LoggedInBefore");

                return RedirectToAction("CheckoutInfo", chOutInfo);
            }

            _context.AddOrUpdate(order);
            await _context.SaveChangesAsync();

            var checkoutInfo = new CheckoutInfo {OrderId = order.OrderId};
            _context.AddOrUpdate(checkoutInfo);

            order.BasketId = basketId;
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("LoggedInBefore", 1);
          
            return View(checkoutInfo);
        }

        [HttpPost]
        public async Task<IActionResult> CheckoutInfo([Bind("Id, OrderId,FirstName,LastName,Email,PostingAddress,PostalCode,City,PhoneNumber")]CheckoutInfo checkoutInfo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.CheckoutInfo.Update(checkoutInfo);

                    await _context.SaveChangesAsync();

                    return RedirectToAction("Payment", checkoutInfo);
                }
                catch (DbUpdateConcurrencyException exc)
                {                   
                    _logger.LogError(exc.ToString());
                }
                return RedirectToAction(nameof(Index));
            }
            return View("LoginOrAnonymous", checkoutInfo);                        
        }
    }
}
