﻿    using System;
//using System.Activities.Statements;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using Memberships.Areas.Admin.Models;
using Memberships.Entities;
using Memberships.Models;

namespace Memberships.Areas.Admin.Extensions
{
    public static class ConversionExtensions
    {
      public static async Task<IEnumerable<ProductModel>> Convert
            (this IEnumerable<Product> products,ApplicationDbContext db)
        {
            if (products.Count().Equals(0))
                return new List<ProductModel>();
            var texts = await db.ProductLinkTexts.ToListAsync();
            var types = await db.ProductTypes.ToListAsync();

            return from p in products
                   select new ProductModel
                   {
                       Id = p.Id,
                       Title = p.Title,
                       Description = p.Description,
                       ImageUrl = p.ImageUrl,
                       ProductLinkTextId = p.ProductLinkTextId,
                       ProductTypeId = p.ProductTypeId,
                       ProductLinkTexts = texts,
                       ProductTypes = types
                   };
        }
        public static async Task<ProductModel> Convert
            (this Product product, ApplicationDbContext db)
        {

            var text = await db.ProductLinkTexts.FirstOrDefaultAsync(
                p => p.Id.Equals(product.ProductLinkTextId));
            var type = await db.ProductTypes.FirstOrDefaultAsync(
                 p => p.Id.Equals(product.ProductTypeId));

            var model = new ProductModel
                   {
                       Id = product.Id,
                       Title = product.Title,
                       Description = product.Description,
                       ImageUrl = product.ImageUrl,
                       ProductLinkTextId = product.ProductLinkTextId,
                       ProductTypeId = product.ProductTypeId,
                       ProductLinkTexts = new List<ProductLinkText>(),
                       ProductTypes = new List<ProductType>(),
            };
            model.ProductLinkTexts.Add(text);
            model.ProductTypes.Add(type);
            return model;
        }
        public static async Task<IEnumerable<ProductItemModel>> Convert
            (this IQueryable<ProductItem> productItems, ApplicationDbContext db)
        {
            if (productItems.Count().Equals(0))
                return new List<ProductItemModel>();


            return await (from pi in productItems
                          select new ProductItemModel
                          {
                              ItemId = pi.ItemId,
                              ProductId = pi.ProductId,
                              ItemTitle = db.Items.FirstOrDefault(i => i.Id.Equals(pi.ItemId)).Title,
                               ProductTitle = db.Products.FirstOrDefault(i => i.Id.Equals(pi.ProductId)).Title
                          }).ToListAsync();
                     
        }
        public static async Task<ProductItemModel> Convert
           (this ProductItem productItem, ApplicationDbContext db)
             {
                    var model = new ProductItemModel
                    {
              
                       ItemId = productItem.ItemId,
                       ProductId = productItem.ProductId,
                       Items = await db.Items.ToListAsync(),
                       Products = await db.Products.ToListAsync()
                    };

                      return model;
            }

        public static async Task<bool> CanChange(
            this ProductItem productItem, ApplicationDbContext db)
        {
            var oldPI = await db.ProductItems.CountAsync(pi =>
            pi.ProductId.Equals(productItem.OldProductId) &&
            pi.ItemId.Equals(productItem.OldItemId));

            var newPI = await db.ProductItems.CountAsync(pi =>
            pi.ProductId.Equals(productItem.ProductId) &&
            pi.ItemId.Equals(productItem.ItemId));

            return oldPI.Equals(1) && newPI.Equals(0);

        }

        public static async Task Change(
            this ProductItem productItem, ApplicationDbContext db)
        {
            var oldProductItem = await db.ProductItems.FirstOrDefaultAsync(pi =>
            pi.ProductId.Equals(productItem.OldProductId) &&
            pi.ItemId.Equals(productItem.OldItemId));


            var newProductItem = await db.ProductItems.FirstOrDefaultAsync(pi =>
            pi.ProductId.Equals(productItem.ProductId) &&
            pi.ItemId.Equals(productItem.ItemId));

            if(oldProductItem !=null && newProductItem == null)
            {
                 newProductItem = new ProductItem
                {
                    ItemId = productItem.ItemId,
                    ProductId = productItem.ProductId
                };

                using (var transction = new TransactionScope(
                    TransactionScopeAsyncFlowOption.Enabled))
                {
                    try
                    {
                        db.ProductItems.Remove(oldProductItem);
                        db.ProductItems.Add(newProductItem);

                        await db.SaveChangesAsync();
                        transction.Complete();
                    }
                    catch { transction.Dispose(); }
                }
            }
        }



    }
    }
