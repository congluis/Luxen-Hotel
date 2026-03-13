using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using LuxenHotel.Data;
using LuxenHotel.Models.Entities.Identity;
using LuxenHotel.Models.Entities.Orders;
using LuxenHotel.Models.ViewModels.Orders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuxenHotel.Areas.Admin.Controllers;

[Route("admin/users")]
public class UserManagerController : AdminBaseController
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ApplicationDbContext _context;


    public UserManagerController(ILogger<AdminBaseController> logger,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ApplicationDbContext context) : base(logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var users = await _context.Users.ToListAsync();
            var userViewModels = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserListViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    FullName = user.FullName,
                    Roles = string.Join(", ", roles)
                });
            }
            SetPageTitle("User Listing");
            return View(userViewModels);
        }
        catch (Exception)
        {
            return View(new List<UserListViewModel>());
        }
    }

    // GET: UserManager/Details/5
    [HttpGet("{id}")]
    public async Task<IActionResult> Details(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Get user's orders
        var orders = await _context.Orders
            .Include(o => o.Accommodation)
            .Where(o => o.UserId == id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var userViewModel = new UserDetailsViewModel
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = user.FullName,
            Roles = roles.ToList(),
            Orders = orders
        };

        SetPageTitle("User Details");
        return View(userViewModel);
    }
}

public class UserListViewModel
{
    public string Id { get; set; }

    [Display(Name = "User Name")]
    public string UserName { get; set; }

    public string Email { get; set; }

    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; }

    [Display(Name = "Full Name")]
    public string FullName { get; set; }

    public string Roles { get; set; }
}

public class UserDetailsViewModel
{
    public string Id { get; set; }

    [Display(Name = "User Name")]
    public string UserName { get; set; }

    public string Email { get; set; }

    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; }

    [Display(Name = "Full Name")]
    public string FullName { get; set; }

    public List<string> Roles { get; set; }

    public List<Orders> Orders { get; set; }
}
