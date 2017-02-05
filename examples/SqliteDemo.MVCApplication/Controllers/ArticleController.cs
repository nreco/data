using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using SqliteDemo.MVCApplication.Db.Context;
using SqliteDemo.MVCApplication.Db.Models;
using SqliteDemo.MVCApplication.Db.Interfaces;
using SqliteDemo.MVCApplication.Db.Repositories;

namespace SqliteDemo.MVCApplication.Controllers
{
	public class ArticleController : Controller {
		IArticleRepository db;

		public ArticleController(ArticleRepository articleRepository) {
			db = articleRepository;
		}

		public IActionResult Add() {
			return View(
				new Article(){
					UsersList = db.GetAllAuthors().ToList<User>()
				}
			);
		}

		[HttpPost]
		public IActionResult Add(Article a) {
			TryValidateModel(a);
			if (ModelState.IsValid) {
				db.Add(a);
				return RedirectToAction("List");
			}
			return View(a);
		}

		public IActionResult Edit(int? id) {
			if (id.HasValue) {
				var article = db.FindById(id.Value);
				article.UsersList = db.GetAllAuthors().ToList<User>();
				return View(
					article
				);
			}
			return NotFound();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Article article) {
			if (ModelState.IsValid) {
				await db.Edit(article);
				return RedirectToAction("List");
			} else {
				return View(article);
			}
		}

		public IActionResult ArticleItem(int id = 0) {
			if (id != 0) {
				return View(
					db.FindById(id)
				);
			}

			return View(new Article());
        }

		public IActionResult Delete(int? id) {
			if (id.HasValue) {
				db.Remove(id.Value);
				return RedirectToAction("List");
			}
			return NotFound();
		}

		public IActionResult List() {
			return View(db.GetArticles());
		}
    }
}
