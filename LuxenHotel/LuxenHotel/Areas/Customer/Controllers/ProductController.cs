// Controllers/ProductController.cs
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using LuxenHotel.Helpers;

[Area("Customer")]
public class ProductController : Controller
{
    private readonly VnPayLibrary _vnPayLibrary;
    private readonly List<Product> _products;

    public ProductController()
    {
        _vnPayLibrary = new VnPayLibrary();

        // Sample product data
        _products = new List<Product>
        {
            new Product { Id = 1, Name = "Laptop", Price = 15000000, Description = "Laptop gaming" },
            new Product { Id = 2, Name = "Phone", Price = 8000000, Description = "Smartphone" }
        };
    }

    // Hiển thị danh sách sản phẩm
    public IActionResult Index()
    {
        return View(_products);
    }

    // Hiển thị form thanh toán
    [HttpGet]
    public IActionResult Payment(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    // Xử lý thanh toán
    [HttpPost]
    public IActionResult Payment(int id, int amount)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        // Tạo thông tin thanh toán
        string vnp_TxnRef = DateTime.Now.Ticks.ToString();
        string vnp_Amount = (product.Price * amount * 100).ToString(); // VnPay yêu cầu nhân 100
        string vnp_OrderInfo = $"Thanh toan san pham {product.Name} so luong {amount}";

        // Thêm các tham số cần thiết cho VnPay
        _vnPayLibrary.AddRequestData("vnp_Version", VnPayConfig.Version);
        _vnPayLibrary.AddRequestData("vnp_Command", VnPayConfig.Command);
        _vnPayLibrary.AddRequestData("vnp_TmnCode", VnPayConfig.TmnCode);
        _vnPayLibrary.AddRequestData("vnp_Amount", vnp_Amount);
        _vnPayLibrary.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        _vnPayLibrary.AddRequestData("vnp_CurrCode", "VND");
        _vnPayLibrary.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString());
        _vnPayLibrary.AddRequestData("vnp_Locale", "vn");
        _vnPayLibrary.AddRequestData("vnp_OrderInfo", vnp_OrderInfo);
        _vnPayLibrary.AddRequestData("vnp_OrderType", VnPayConfig.OrderType);
        _vnPayLibrary.AddRequestData("vnp_ReturnUrl", VnPayConfig.ReturnUrl);
        _vnPayLibrary.AddRequestData("vnp_TxnRef", vnp_TxnRef);

        // Tạo URL thanh toán
        string paymentUrl = _vnPayLibrary.CreateRequestUrl(VnPayConfig.PayUrl, VnPayConfig.SecretKey);

        return Redirect(paymentUrl);
    }

    // Xử lý callback từ VnPay
    public IActionResult Return()
    {
        foreach (var key in Request.Query.Keys)
        {
            _vnPayLibrary.AddResponseData(key, Request.Query[key]);
        }

        bool isValidSignature = _vnPayLibrary.ValidateSignature(
            Request.Query["vnp_SecureHash"],
            VnPayConfig.SecretKey
        );

        var model = new PaymentReturnModel
        {
            TransactionId = Request.Query["vnp_TxnRef"],
            Amount = decimal.Parse(Request.Query["vnp_Amount"]) / 100,
            OrderInfo = Request.Query["vnp_OrderInfo"],
            ResponseCode = Request.Query["vnp_ResponseCode"],
            IsSuccess = isValidSignature && Request.Query["vnp_ResponseCode"] == "00"
        };

        return View(model);
    }
}

// Model cho return view
public class PaymentReturnModel
{
    public string TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string OrderInfo { get; set; }
    public string ResponseCode { get; set; }
    public bool IsSuccess { get; set; }
}

// Models/Product.cs
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
}