using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace e621TagGetter
{
	/// <summary>
	/// Some friends wanted to try playing scribbl.io with a bunch of tags from e621 so this just gets them all
	/// </summary>
	public sealed class Program
	{
		private const int c_waitTime = 2000; // milliseconds between api requests
		private const int c_tagsPerPage = 1000; // 1000 is the maximum
		private static readonly int[] s_categories = { 0, 4, 5 };

		public static void Main(string[] _)
		{
			var allTags = GetTags();

			File.WriteAllText("allTags.json", JsonConvert.SerializeObject(allTags));

			WriteTagCategoryFiles(allTags);
		}

		private static List<Tag> GetTags()
		{
			HttpClient client = new HttpClient
			{
				BaseAddress = new Uri("https://e621.net/")
			};
			client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
			// custom user agent required by API
			client.DefaultRequestHeaders.UserAgent.ParseAdd("TagGetter/0.1 (by Pyritie)");


			var allTags = new List<Tag>();
			foreach (int category in s_categories)
			{
				int page = 0;
				while (true)
				{
					Console.WriteLine($"Getting page {page} of category {CategoryName(category)}...");

					var response = client.GetAsync($"tags.json?limit={c_tagsPerPage}&page={page++}&search[category]={category}&search[hide_empty]=true").Result;
					if (response.IsSuccessStatusCode)
					{
						string json = response.Content.ReadAsStringAsync().Result;
						if (json == "{\"tags\":[]}")
						{
							break;
						}

						var tags = JsonConvert.DeserializeObject<List<Tag>>(json);
						allTags.AddRange(tags);
					}
					else
					{
						Debugger.Break();
					}

					// wait a bit bitween requests
					Thread.Sleep(c_waitTime);
				}
				Thread.Sleep(c_waitTime);
			}

			return allTags;
		}

		private static void WriteTagCategoryFiles(IEnumerable<Tag> allTags)
		{
			foreach (var group in allTags.GroupBy(t => t.category))
			{
				File.WriteAllText(CategoryName(group.Key) + "Tags.txt", 
					string.Join('\n', group.OrderByDescending(t => t.post_count).Select(t => t.name.Replace('_', ' '))));
			}
		}

		private static string CategoryName(int categoryId) => categoryId switch
		{
			0 => "general",
			4 => "character",
			5 => "species",
			_ => "idk",
		};
	}

	[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "it's for json")]
	public class Tag
	{
		/// <summary>
		/// unique id
		/// </summary>
		public int id { get; set; }
		/// <summary>
		/// name of the tag
		/// </summary>
		public string name { get; set; }
		/// <summary>
		/// 0 - general
		/// 1 - artist
		/// 3 - copyright
		/// 4 - character
		/// 5 - species
		/// 6 - invalid
		/// 7 - meta
		/// 8 - lore
		/// </summary>
		public int category { get; set; }
		/// <summary>
		/// number of posts with this tag
		/// </summary>
		public int post_count { get; set; }
	}
}
