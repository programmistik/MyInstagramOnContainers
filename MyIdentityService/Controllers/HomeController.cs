﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using MyIdentityService.Helpers;
using MyIdentityService.Models;
using MyIdentityService.Services;
using MyIdentityService.ViewModels;
using Newtonsoft.Json;


namespace MyIdentityService.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProfileService _profileService;

        public HomeController(ProfileService profileService)
        {
            _profileService = profileService;
        }

        public async Task<IActionResult> Index(int spage = 1)
        {
            var page = spage;
            int pageSize = 3;   // количество элементов на странице

            //CONNECT
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("http://localhost:5001");
            if (disco.IsError)
            {
                //Console.WriteLine(disco.Error);
                //return;
            }

            //GET TOKEN
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,

                ClientId = "client",
                ClientSecret = "secret",
                Scope = "Post"
            });
            if (tokenResponse.IsError)
            {
                //Console.WriteLine(tokenResponse.Error);
                //return;
            }


            //CALL API
            client.SetBearerToken(tokenResponse.AccessToken);


            var response = await client.GetAsync("http://localhost:5012/post/");
            if (!response.IsSuccessStatusCode)
            {
                //  Console.WriteLine(response.StatusCode);
                //Result.Profiles = new List<Profile>();
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                var dummyItems = JsonConvert.DeserializeObject<List<Post>>(content);
                var sortedItems = dummyItems.OrderByDescending(x => x.LikesProfileId.Count()).ToList();
                //IQueryable<Post> source = db.Users.Include(x => x.Company);
                var count = sortedItems.Count();

                var items = sortedItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                PageViewModel pageViewModel = new PageViewModel(count, page, pageSize);
                List<PostViewModel> Result = new List<PostViewModel>();

                if (items != null)
                    foreach (var item in items)
                    {
                        var prof = _profileService.Get(item.ProfileId);
                        var newItm = new PostViewModel
                        {
                            Post = item,
                            Profile = prof
                        };
                        Result.Add(newItm);

                    }
                IndexViewModel viewModel = new IndexViewModel
                {
                    PageViewModel = pageViewModel,
                    Posts = Result
                };
                return View(viewModel);
            }


           

           // var dummyItems = Enumerable.Range(1, 150).Select(x => "Item " + x);
            //var pager = new Pager(dummyItems.Count(), page);

            //var viewModel = new IndexViewModel
            //{
            //    Items = dummyItems.Skip((pager.CurrentPage - 1) * pager.PageSize).Take(pager.PageSize),
            //    Pager = pager
            //};

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Search(string search)
        {
            if (search?.Length > 0)
            {
                var res = await SearchAsync(search);
                return View(res);
            }
            else
            {
                return RedirectToAction("Index");
            }

            
        }

        private async Task<SearchingResult> SearchAsync(string str)
        {
            var Result = new SearchingResult();

            //CONNECT
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("http://localhost:5001");
            if (disco.IsError)
            {
                //Console.WriteLine(disco.Error);
                //return;
            }

            //GET TOKEN
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,

                ClientId = "client",
                ClientSecret = "secret",
                Scope = "Post"
            });
            if (tokenResponse.IsError)
            {
                //Console.WriteLine(tokenResponse.Error);
                //return;
            }
            

            //CALL API
            client.SetBearerToken(tokenResponse.AccessToken);

            
            var response = await client.GetAsync("http://localhost:5012/profile/" + str);
            if (!response.IsSuccessStatusCode)
            {
                //  Console.WriteLine(response.StatusCode);
                Result.Profiles = new List<Profile>();
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Result.Profiles = JsonConvert.DeserializeObject<List<Profile>>(content);
            }


            var responsePost = await client.GetAsync("http://localhost:5012/search/" + str.ToLower());
            if (!responsePost.IsSuccessStatusCode)
            {
                //  Console.WriteLine(response.StatusCode);
            }
            else
            {
                var contentPost = await responsePost.Content.ReadAsStringAsync();
                var rr = JsonConvert.DeserializeObject<List<Post>>(contentPost);
                Result.Posts = rr;
            }

            return Result;
        }
    }
}
