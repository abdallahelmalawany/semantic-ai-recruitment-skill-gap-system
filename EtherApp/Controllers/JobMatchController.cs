using EtherApp.Controllers.Base;
using EtherApp.Data.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EtherApp.Controllers
{
    public class JobMatchController : BaseController
    {
        private readonly IInterestService _interestService;
        private readonly HttpClient _httpClient;

        public JobMatchController(IInterestService interestService, IHttpClientFactory httpClientFactory)
        {
            _interestService = interestService;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId() ?? 0;
            if (userId == 0)
                return RedirectToAction("Login", "Authentication");

            // Get user interests/keywords
            var userInterests = await _interestService.GetUserInterestsAsync(userId);
            var skillsText = string.Join(", ", userInterests.Select(i => i.Name + " " + i.Keywords));

            // Call Python AI API
            var matches = await GetJobMatchesAsync(skillsText);

            ViewBag.Skills = skillsText;
            return View(matches);
        }

        private async Task<List<JobMatchResult>> GetJobMatchesAsync(string skills)
        {
            try
            {
                var payload = JsonSerializer.Serialize(new { skills });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("http://localhost:8000/match", content);
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MatchResponse>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Matches ?? new List<JobMatchResult>();
            }
            catch
            {
                return new List<JobMatchResult>();
            }
        }
    }

    // Response Models
    public class MatchResponse
    {
        public List<JobMatchResult> Matches { get; set; }
    }

    public class JobMatchResult
    {
        public string Job_Title { get; set; }
        public double Match_Score { get; set; }
        public string Skills { get; set; }
    }
}