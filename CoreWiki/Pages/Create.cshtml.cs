using CoreWiki.Areas.Identity;
using CoreWiki.Core.Interfaces;
using CoreWiki.Models;
using CoreWiki.Application;
using CoreWiki.Application.Articles.Commands;
using CoreWiki.Helpers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NodaTime;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CoreWiki.Core.Domain;
using System.Globalization;

namespace CoreWiki.Pages
{

	[Authorize(Policy = PolicyConstants.CanWriteArticles)]
	public class CreateModel : PageModel
	{
		private readonly IMediator _mediator;
		private readonly IArticleRepository _articleRepo;
		public ILogger Logger { get; private set; }

		public CreateModel(IMediator mediator, IArticleRepository articleRepo, ILoggerFactory loggerFactory)
		{
			_mediator = mediator;
			_articleRepo = articleRepo;
			this.Logger = loggerFactory.CreateLogger("CreatePage");
		}

		public IActionResult OnGet(string slug = "")
		{

			Article = new Models.ArticleCreateDTO()
			{
				Topic = SlugToTopic(slug ?? "")
			};

			return Page();
		}

		[BindProperty]
		public Models.ArticleCreateDTO Article { get; set; }   //CoreWiki.Application DTO

		public async Task<IActionResult> OnPostAsync()
		{
			var slug = UrlHelpers.URLFriendly(Article.Topic);
			if (string.IsNullOrWhiteSpace(slug))
			{
				ModelState.AddModelError("Article.Topic", "The Topic must contain at least one alphanumeric character.");
				return Page();
			}

			if (!ModelState.IsValid) { return Page(); }

			Logger.LogWarning($"Creating page with slug: {slug}");

			if (await _articleRepo.IsTopicAvailable(slug, 0))
			{
				ModelState.AddModelError("Article.Topic", "This Title already exists.");
				return Page();
			}

			// -- THIS NEEDS TO BE FIXED --  

			Article.AuthorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
			Article.AuthorName = User.Identity.Name;
			Article.Slug = slug;

			var _articleLinks = await _mediator.Send(new CreateNewArticleCommand(Article));

			if (_articleLinks.Count > 0)
			{
				return RedirectToPage("CreateArticleFromLink", new { id = slug });
			}

			return Redirect($"/wiki/{slug}");
		}

		public static string SlugToTopic(string slug)
		{
			if (string.IsNullOrEmpty(slug))
			{
				return "";
			}

			var textInfo = new CultureInfo("en-US", false).TextInfo;
			var outValue = textInfo.ToTitleCase(slug);

			return outValue.Replace("-", " ");

		}
	}
}
