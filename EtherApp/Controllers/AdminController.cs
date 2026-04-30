using EtherApp.Data;
using EtherApp.Data.Models;
using EtherApp.Data.Services;
using EtherApp.Data.Services.Interfaces;
using EtherApp.ViewModels.Admin;
using EtherApp.ViewModels.Interests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EtherApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDBContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IContentAnalysisService _contentAnalysisService;

        public AdminController(
            AppDBContext context,
            UserManager<User> userManager,
            IContentAnalysisService contentAnalysisService)
        {
            _context = context;
            _userManager = userManager;
            _contentAnalysisService = contentAnalysisService;
        }

        // Shows admin dashboard with all tabs
        public async Task<IActionResult> Index(int activeTab = 0)
        {
            // Get reported posts for Tab 2
            var flaggedPosts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Reports)
                .Where(p => p.NrOfReports >= 5 && !p.IsDeleted)
                .OrderByDescending(p => p.Reports.Count)
                .ToListAsync();
                
            foreach (var post in flaggedPosts)
            {
                post.NrOfReports = post.Reports.Count;
            }
            
            // Get moderation queue posts for Tab 3
            var postsAwaitingModeration = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.RequiresModeration && !p.IsDeleted)
                .OrderByDescending(p => p.DateCreated)
                .ToListAsync();
            
            ViewBag.ModerationPosts = postsAwaitingModeration;
            ViewBag.ModerationCount = postsAwaitingModeration.Count;
            ViewBag.ActiveTab = activeTab;
            
            return View(flaggedPosts);
        }
        
        [HttpPost]
        public async Task<IActionResult> TestContentAnalysis(ContentAnalysisViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Content))
            {
                return RedirectToAction(nameof(Index), new { activeTab = 3 });
            }
            
            // Analyze the content using the service
            var analysisResults = await _contentAnalysisService.AnalyzeContentAsync(model.Content);
            
            // Get all interests to match IDs to names
            var interests = await _context.Interests.ToListAsync();
            
            // Convert the analysis results to InterestScore objects
            var results = new List<InterestScore>();
            foreach (var result in analysisResults)
            {
                var interest = interests.FirstOrDefault(i => i.Id == result.InterestId);
                if (interest != null)
                {
                    results.Add(new InterestScore
                    {
                        Interest = interest.Name,
                        Score = result.Score,
                        Keywords = interest.Keywords ?? string.Empty
                    });
                }
            }
            
            model.Results = results;
            
            // Get data for all tabs
            var flaggedPosts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Reports)
                .Where(p => p.NrOfReports > 5 && !p.IsDeleted)
                .OrderByDescending(p => p.Reports.Count)
                .ToListAsync();
                
            foreach (var post in flaggedPosts)
            {
                post.NrOfReports = post.Reports.Count;
            }
            
            var postsAwaitingModeration = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.RequiresModeration && !p.IsDeleted)
                .OrderByDescending(p => p.DateCreated)
                .ToListAsync();
            
            ViewBag.ModerationPosts = postsAwaitingModeration;
            ViewBag.ModerationCount = postsAwaitingModeration.Count;
            ViewBag.ContentAnalysisModel = model;
            ViewBag.ActiveTab = 3; // Set content analysis tab as active
            
            return View("Index", flaggedPosts);
        }
        
        // POST: Admin/ApprovePost
        [HttpPost]
        public async Task<IActionResult> ApprovePost(int postId)
        {
            var post = await _context.Posts
                .Include(p => p.Reports)
                .FirstOrDefaultAsync(p => p.Id == postId);
                
            if (post != null)
            {
                // Remove all reports for this post
                _context.Reports.RemoveRange(post.Reports);
                post.NrOfReports = 0;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Post has been approved and all reports have been cleared.";
            }
            else
            {
                TempData["ErrorMessage"] = "Post not found.";
            }
            
            return RedirectToAction(nameof(Index), new { activeTab = 1 });
        }
        
        // POST: Admin/DeletePost
        [HttpPost]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                post.IsDeleted = true;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Post has been deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Post not found.";
            }
            
            return RedirectToAction(nameof(Index), new { activeTab = 1 });
        }

        // GET: Admin/SearchUsers
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                ViewBag.SearchTerm = string.Empty;
                ViewBag.SearchResults = new List<UserWithRolesVM>();
                return RedirectToAction(nameof(Index));
            }

            var users = await _userManager.Users
                .Where(u => (u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm)) && !u.IsDeleted)
                .ToListAsync();

            var usersWithRoles = new List<UserWithRolesVM>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new UserWithRolesVM
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Roles = roles.ToList()
                });
            }

            // Get data for all tabs
            var flaggedPosts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Reports)
                .Where(p => p.NrOfReports > 5 && !p.IsDeleted)
                .OrderByDescending(p => p.Reports.Count)
                .ToListAsync();
                
            foreach (var post in flaggedPosts)
            {
                post.NrOfReports = post.Reports.Count;
            }
            
            var postsAwaitingModeration = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.RequiresModeration && !p.IsDeleted)
                .OrderByDescending(p => p.DateCreated)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SearchResults = usersWithRoles;
            ViewBag.ModerationPosts = postsAwaitingModeration;
            ViewBag.ModerationCount = postsAwaitingModeration.Count;
            ViewBag.ActiveTab = 0; // Set user management tab as active
            
            return View("Index", flaggedPosts);
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }
            
            // Check if user is admin - prevent deleting admins
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["ErrorMessage"] = "Cannot delete admin users.";
                return RedirectToAction(nameof(Index));
            }
            
            // Hard delete the user
            var result = await _userManager.DeleteAsync(user);
            
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User has been permanently deleted from the system.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/ApproveModeratedPost
        [HttpPost]
        public async Task<IActionResult> ApproveModeratedPost(int postId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "User not authenticated.";
                return RedirectToAction(nameof(Index), new { activeTab = 2 });
            }

            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                post.RequiresModeration = false;
                post.IsHidden = false;
                post.ModeratedAt = DateTime.Now;
                post.ModeratorId = currentUser.Id;
                post.DateUpdated = DateTime.Now;
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Post has been approved and is now visible to users.";
            }
            else
            {
                TempData["ErrorMessage"] = "Post not found.";
            }
            
            return RedirectToAction(nameof(Index), new { activeTab = 2 });
        }

        // POST: Admin/RejectModeratedPost
        [HttpPost]
        public async Task<IActionResult> RejectModeratedPost(int postId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "User not authenticated.";
                return RedirectToAction(nameof(Index), new { activeTab = 2 });
            }

            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                post.IsDeleted = true;
                post.ModeratedAt = DateTime.Now;
                post.ModeratorId = currentUser.Id;
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Post has been rejected and deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = "Post not found.";
            }
            
            return RedirectToAction(nameof(Index), new { activeTab = 2 });
        }
    }
}