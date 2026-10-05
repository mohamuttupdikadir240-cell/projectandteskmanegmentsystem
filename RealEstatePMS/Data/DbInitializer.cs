using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        try
        {
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }
        }
        catch
        {
            await context.Database.EnsureCreatedAsync();
        }

        var environment = services.GetRequiredService<IWebHostEnvironment>();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await SeedDefaultPermissionsAsync(context);

        // Admin
        var admin = await EnsureUserAsync(userManager, "admin@realestate.com", "Admin@12345", "System Administrator", Roles.Admin);
        var pm = await EnsureUserAsync(userManager, "pm@realestate.com", "Manager@12345", "Amina Yusuf", Roles.ProjectManager);
        var agent1 = await EnsureUserAsync(userManager, "agent1@realestate.com", "Agent@12345", "Mohamed Ali", Roles.SalesAgent);
        var agent2 = await EnsureUserAsync(userManager, "agent2@realestate.com", "Agent@12345", "Said Ibrahim", Roles.SalesAgent);
        var accountant = await EnsureUserAsync(userManager, "accountant@realestate.com", "Accountant@12345", "Hodan Warsame", Roles.Accountant);
        var customerUser = await EnsureUserAsync(userManager, "customer@realestate.com", "Customer@12345", "Ahmed Hassan", Roles.Customer);

        if (context.Projects.Any())
            return; // sample data already seeded

        var project1 = new Project
        {
            Name = "Green Valley Residences",
            Code = "PRJ-001",
            Location = "Mogadishu",
            Description = "A modern residential complex with apartments and villas surrounded by green spaces.",
            StartDate = DateTime.UtcNow.AddMonths(-8),
            ExpectedEndDate = DateTime.UtcNow.AddMonths(4),
            Budget = 2500000,
            ProjectManagerId = pm.Id,
            Status = ProjectStatus.InProgress,
            ProgressPercentage = 70
        };

        var project2 = new Project
        {
            Name = "Horizon Towers",
            Code = "PRJ-002",
            Location = "Hargeisa",
            Description = "High-rise commercial and residential towers in the heart of the city.",
            StartDate = DateTime.UtcNow.AddMonths(-2),
            ExpectedEndDate = DateTime.UtcNow.AddMonths(10),
            Budget = 4200000,
            ProjectManagerId = pm.Id,
            Status = ProjectStatus.Planning,
            ProgressPercentage = 15
        };

        var project3 = new Project
        {
            Name = "Somali Plaza",
            Code = "PRJ-003",
            Location = "Bosaso",
            Description = "Retail and office plaza with shops and business suites.",
            StartDate = DateTime.UtcNow.AddMonths(-14),
            ExpectedEndDate = DateTime.UtcNow.AddMonths(-1),
            Budget = 1500000,
            ProjectManagerId = pm.Id,
            Status = ProjectStatus.Completed,
            ProgressPercentage = 100
        };

        var project4 = new Project
        {
            Name = "City Center Complex",
            Code = "PRJ-004",
            Location = "Mogadishu",
            Description = "Mixed-use apartments and villas close to the city center.",
            StartDate = DateTime.UtcNow.AddMonths(-5),
            ExpectedEndDate = DateTime.UtcNow.AddMonths(7),
            Budget = 3100000,
            ProjectManagerId = pm.Id,
            Status = ProjectStatus.InProgress,
            ProgressPercentage = 45
        };

        var project5 = new Project
        {
            Name = "Beach Front Villas",
            Code = "PRJ-005",
            Location = "Kismayo",
            Description = "Luxury beachfront villas currently paused pending permit renewal.",
            StartDate = DateTime.UtcNow.AddMonths(-3),
            ExpectedEndDate = DateTime.UtcNow.AddMonths(9),
            Budget = 5000000,
            ProjectManagerId = pm.Id,
            Status = ProjectStatus.OnHold,
            ProgressPercentage = 20
        };

        context.Projects.AddRange(project1, project2, project3, project4, project5);
        await context.SaveChangesAsync();

        var properties = new List<Property>
        {
            // Green Valley Residences
            new() { Code = "P-001", ProjectId = project1.Id, Type = PropertyType.Apartment, Building = "Building A", Floor = "2", UnitNumber = "A-2-1", Area = 120, Bedrooms = 3, Bathrooms = 2, Price = 120000, Status = PropertyStatus.Available, Description = "Modern apartment with sea view." },
            new() { Code = "P-002", ProjectId = project1.Id, Type = PropertyType.Villa, Building = "Villa Row", Floor = "1", UnitNumber = "V-01", Area = 320, Bedrooms = 5, Bathrooms = 4, Price = 350000, Status = PropertyStatus.Available, Description = "Spacious family villa with garden." },
            new() { Code = "P-006", ProjectId = project1.Id, Type = PropertyType.Apartment, Building = "Building A", Floor = "3", UnitNumber = "A-3-2", Area = 118, Bedrooms = 3, Bathrooms = 2, Price = 135000, Status = PropertyStatus.Available, Description = "Corner apartment with balcony." },
            new() { Code = "P-007", ProjectId = project1.Id, Type = PropertyType.Apartment, Building = "Building B", Floor = "1", UnitNumber = "B-1-4", Area = 105, Bedrooms = 2, Bathrooms = 2, Price = 128000, Status = PropertyStatus.Reserved, Description = "Reserved pending contract signature." },
            new() { Code = "P-016", ProjectId = project1.Id, Type = PropertyType.Apartment, Building = "Building B", Floor = "2", UnitNumber = "B-2-1", Area = 100, Bedrooms = 2, Bathrooms = 1, Price = 110000, Status = PropertyStatus.Available, Description = "Compact 2-bedroom apartment." },

            // Horizon Towers
            new() { Code = "P-003", ProjectId = project2.Id, Type = PropertyType.Office, Building = "Tower 1", Floor = "5", UnitNumber = "T1-501", Area = 90, Bedrooms = 0, Bathrooms = 1, Price = 200000, Status = PropertyStatus.Available, Description = "Prime office space." },
            new() { Code = "P-008", ProjectId = project2.Id, Type = PropertyType.Apartment, Building = "Tower 2", Floor = "8", UnitNumber = "T2-801", Area = 130, Bedrooms = 3, Bathrooms = 2, Price = 180000, Status = PropertyStatus.UnderConstruction, Description = "High-floor apartment, under construction." },
            new() { Code = "P-009", ProjectId = project2.Id, Type = PropertyType.Shop, Building = "Tower 1", Floor = "G", UnitNumber = "T1-G3", Area = 55, Bedrooms = 0, Bathrooms = 1, Price = 95000, Status = PropertyStatus.Available, Description = "Ground floor retail unit." },
            new() { Code = "P-018", ProjectId = project2.Id, Type = PropertyType.Office, Building = "Tower 1", Floor = "6", UnitNumber = "T1-601", Area = 95, Bedrooms = 0, Bathrooms = 1, Price = 220000, Status = PropertyStatus.Available, Description = "Corner office with city view." },

            // Somali Plaza
            new() { Code = "P-004", ProjectId = project3.Id, Type = PropertyType.Shop, Building = "Plaza", Floor = "G", UnitNumber = "G-12", Area = 60, Bedrooms = 0, Bathrooms = 1, Price = 150000, Status = PropertyStatus.Available, Description = "Ground floor retail shop." },
            new() { Code = "P-005", ProjectId = project3.Id, Type = PropertyType.Land, Building = "-", Floor = "-", UnitNumber = "L-09", Area = 500, Bedrooms = 0, Bathrooms = 0, Price = 450000, Status = PropertyStatus.Available, Description = "Land plot ready for development." },
            new() { Code = "P-010", ProjectId = project3.Id, Type = PropertyType.Office, Building = "Plaza", Floor = "2", UnitNumber = "P-201", Area = 100, Bedrooms = 0, Bathrooms = 1, Price = 210000, Status = PropertyStatus.Available, Description = "Second-floor business suite." },
            new() { Code = "P-017", ProjectId = project3.Id, Type = PropertyType.Shop, Building = "Plaza", Floor = "G", UnitNumber = "G-14", Area = 48, Bedrooms = 0, Bathrooms = 1, Price = 90000, Status = PropertyStatus.Available, Description = "Small retail kiosk." },

            // City Center Complex
            new() { Code = "P-011", ProjectId = project4.Id, Type = PropertyType.Apartment, Building = "Block C", Floor = "4", UnitNumber = "C-4-2", Area = 115, Bedrooms = 3, Bathrooms = 2, Price = 140000, Status = PropertyStatus.Available, Description = "Bright 3-bedroom apartment." },
            new() { Code = "P-012", ProjectId = project4.Id, Type = PropertyType.Apartment, Building = "Block C", Floor = "5", UnitNumber = "C-5-1", Area = 108, Bedrooms = 2, Bathrooms = 2, Price = 130000, Status = PropertyStatus.Available, Description = "2-bedroom apartment near amenities." },
            new() { Code = "P-013", ProjectId = project4.Id, Type = PropertyType.Villa, Building = "Villa Court", Floor = "1", UnitNumber = "VC-03", Area = 290, Bedrooms = 4, Bathrooms = 3, Price = 380000, Status = PropertyStatus.Available, Description = "4-bedroom villa with private yard." },

            // Beach Front Villas
            new() { Code = "P-014", ProjectId = project5.Id, Type = PropertyType.Villa, Building = "Beach Row", Floor = "1", UnitNumber = "BR-01", Area = 400, Bedrooms = 5, Bathrooms = 4, Price = 500000, Status = PropertyStatus.Available, Description = "Beachfront villa with private pool." },
            new() { Code = "P-015", ProjectId = project5.Id, Type = PropertyType.Villa, Building = "Beach Row", Floor = "1", UnitNumber = "BR-02", Area = 390, Bedrooms = 5, Bathrooms = 4, Price = 480000, Status = PropertyStatus.UnderConstruction, Description = "Beachfront villa, construction paused." },
        };

        context.Properties.AddRange(properties);
        await context.SaveChangesAsync();

        // Sample placeholder images (real SVG files, no external downloads needed)
        var webRoot = environment.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        }
        var propertyImagesFolder = Path.Combine(webRoot, "uploads", "properties");
        Directory.CreateDirectory(propertyImagesFolder);

        foreach (var property in properties)
        {
            var imagePath = CreatePlaceholderPropertyImage(propertyImagesFolder, property.Code, property.Type);
            context.PropertyImages.Add(new PropertyImage { PropertyId = property.Id, ImagePath = imagePath });
        }
        await context.SaveChangesAsync();

        var customers = new List<Customer>
        {
            new() { Name = "Ahmed Hassan", Phone = "+252612345667", Email = "ahmed.hassan@example.com", Address = "Mogadishu", Nationality = "Somali", IdNumber = "ID-1001", DateOfBirth = new DateTime(1988, 4, 12), ApplicationUserId = customerUser.Id },
            new() { Name = "Fatima Ali", Phone = "+252611777777", Email = "fatima.ali@example.com", Address = "Mogadishu", Nationality = "Somali", IdNumber = "ID-1002", DateOfBirth = new DateTime(1992, 9, 3) },
            new() { Name = "Mohamed Yusuf", Phone = "+252611888888", Email = "mohamed.yusuf@example.com", Address = "Kismayo", Nationality = "Somali", IdNumber = "ID-1003", DateOfBirth = new DateTime(1985, 1, 20) },
            new() { Name = "Maryam Ahmed", Phone = "+252614448888", Email = "maryam.ahmed@example.com", Address = "Hargeisa", Nationality = "Somali", IdNumber = "ID-1004", DateOfBirth = new DateTime(1995, 6, 30) },
            new() { Name = "Yusuf Ali", Phone = "+252613332222", Email = "yusuf.ali@example.com", Address = "Bosaso", Nationality = "Somali", IdNumber = "ID-1005", DateOfBirth = new DateTime(1980, 11, 2) },
            new() { Name = "Hassan Mohamed", Phone = "+252615556677", Email = "hassan.mohamed@example.com", Address = "Mogadishu", Nationality = "Somali", IdNumber = "ID-1006", DateOfBirth = new DateTime(1990, 3, 18) },
            new() { Name = "Amina Farah", Phone = "+252617778899", Email = "amina.farah@example.com", Address = "Hargeisa", Nationality = "Somali", IdNumber = "ID-1007", DateOfBirth = new DateTime(1993, 7, 25) },
            new() { Name = "Abdullahi Nur", Phone = "+252619990011", Email = "abdullahi.nur@example.com", Address = "Kismayo", Nationality = "Somali", IdNumber = "ID-1008", DateOfBirth = new DateTime(1987, 2, 14) },
            new() { Name = "Sagal Warsame", Phone = "+252612223344", Email = "sagal.warsame@example.com", Address = "Bosaso", Nationality = "Somali", IdNumber = "ID-1009", DateOfBirth = new DateTime(1991, 5, 9) },
            new() { Name = "Khadija Omar", Phone = "+252614445566", Email = "khadija.omar@example.com", Address = "Mogadishu", Nationality = "Somali", IdNumber = "ID-1010", DateOfBirth = new DateTime(1996, 12, 1) },
        };

        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        // Properties turned into sales/rentals (looked up by code for clarity)
        Property ByCode(string code) => properties.First(p => p.Code == code);

        var salesData = new (string PropertyCode, int CustomerIndex, ApplicationUser Agent, int MonthsAgo, decimal Discount, decimal Deposit, PaymentStatus Status)[]
        {
            ("P-001", 0, agent1, 6, 5000, 35000, PaymentStatus.Partial),
            ("P-006", 3, agent1, 4, 0, 135000, PaymentStatus.Paid),
            ("P-010", 2, agent2, 2, 10000, 50000, PaymentStatus.Partial),
            ("P-013", 4, agent2, 1, 15000, 100000, PaymentStatus.Partial),
        };

        var sales = new List<Sale>();
        foreach (var s in salesData)
        {
            var property = ByCode(s.PropertyCode);
            property.Status = PropertyStatus.Sold;
            var finalPrice = property.Price - s.Discount;

            var sale = new Sale
            {
                CustomerId = customers[s.CustomerIndex].Id,
                PropertyId = property.Id,
                SalesAgentId = s.Agent.Id,
                SaleDate = DateTime.UtcNow.AddMonths(-s.MonthsAgo),
                TotalPrice = property.Price,
                Discount = s.Discount,
                FinalPrice = finalPrice,
                Deposit = s.Deposit,
                RemainingBalance = finalPrice - s.Deposit,
                PaymentStatus = s.Status
            };
            sales.Add(sale);
        }

        context.Sales.AddRange(sales);
        await context.SaveChangesAsync();

        for (int i = 0; i < sales.Count; i++)
        {
            context.Payments.Add(new Payment
            {
                CustomerId = sales[i].CustomerId,
                SaleId = sales[i].Id,
                Amount = sales[i].Deposit,
                PaymentDate = sales[i].SaleDate,
                Method = i % 2 == 0 ? PaymentMethod.BankTransfer : PaymentMethod.Cash,
                ReferenceNumber = $"TRX-{1000 + i}",
                Notes = "Initial deposit"
            });
        }

        // A second installment on the first sale, a couple of months later, for chart variety
        context.Payments.Add(new Payment
        {
            CustomerId = sales[0].CustomerId,
            SaleId = sales[0].Id,
            Amount = 20000,
            PaymentDate = sales[0].SaleDate.AddMonths(2),
            Method = PaymentMethod.MobileMoney,
            ReferenceNumber = "TRX-1010",
            Notes = "Second installment"
        });

        var rentalsData = new (string PropertyCode, int CustomerIndex, int MonthsAgo, decimal MonthlyRent, decimal Deposit)[]
        {
            ("P-003", 1, 3, 1500, 3000),
            ("P-017", 5, 2, 800, 1600),
            ("P-012", 6, 1, 1200, 2400),
        };

        var rentals = new List<Rental>();
        foreach (var r in rentalsData)
        {
            var property = ByCode(r.PropertyCode);
            property.Status = PropertyStatus.Rented;

            rentals.Add(new Rental
            {
                CustomerId = customers[r.CustomerIndex].Id,
                PropertyId = property.Id,
                MonthlyRent = r.MonthlyRent,
                StartDate = DateTime.UtcNow.AddMonths(-r.MonthsAgo),
                EndDate = DateTime.UtcNow.AddMonths(12 - r.MonthsAgo),
                Deposit = r.Deposit,
                PaymentStatus = PaymentStatus.Partial,
                ContractStatus = ContractStatus.Active
            });
        }

        context.Rentals.AddRange(rentals);
        await context.SaveChangesAsync();

        foreach (var rental in rentals)
        {
            context.Payments.Add(new Payment
            {
                CustomerId = rental.CustomerId,
                RentalId = rental.Id,
                Amount = rental.Deposit,
                PaymentDate = rental.StartDate,
                Method = PaymentMethod.Cash,
                Notes = "Security deposit"
            });
        }

        // Sample tasks across all projects
        context.ProjectTasks.AddRange(
            new ProjectTask { Name = "Foundation works", ProjectId = project1.Id, AssignedEmployeeId = pm.Id, StartDate = DateTime.UtcNow.AddMonths(-8), DueDate = DateTime.UtcNow.AddMonths(-6), Priority = TaskPriority.High, Status = ProjectTaskStatus.Completed, Progress = 100 },
            new ProjectTask { Name = "Electrical installation", ProjectId = project1.Id, AssignedEmployeeId = pm.Id, StartDate = DateTime.UtcNow.AddMonths(-2), DueDate = DateTime.UtcNow.AddMonths(1), Priority = TaskPriority.Medium, Status = ProjectTaskStatus.InProgress, Progress = 55 },
            new ProjectTask { Name = "Site survey", ProjectId = project2.Id, AssignedEmployeeId = pm.Id, StartDate = DateTime.UtcNow.AddMonths(-1), DueDate = DateTime.UtcNow.AddDays(-5), Priority = TaskPriority.Urgent, Status = ProjectTaskStatus.Delayed, Progress = 40 },
            new ProjectTask { Name = "Structural approval", ProjectId = project2.Id, AssignedEmployeeId = pm.Id, StartDate = DateTime.UtcNow.AddDays(-10), DueDate = DateTime.UtcNow.AddDays(20), Priority = TaskPriority.High, Status = ProjectTaskStatus.Pending, Progress = 0 },
            new ProjectTask { Name = "Final inspection", ProjectId = project3.Id, AssignedEmployeeId = pm.Id, StartDate = DateTime.UtcNow.AddMonths(-2), DueDate = DateTime.UtcNow.AddMonths(-1), Priority = TaskPriority.Medium, Status = ProjectTaskStatus.Completed, Progress = 100 },
            new ProjectTask { Name = "Plumbing rough-in", ProjectId = project4.Id, AssignedEmployeeId = pm.Id, StartDate = DateTime.UtcNow.AddMonths(-3), DueDate = DateTime.UtcNow.AddDays(-15), Priority = TaskPriority.High, Status = ProjectTaskStatus.Delayed, Progress = 65 },
            new ProjectTask { Name = "Interior finishing", ProjectId = project4.Id, AssignedEmployeeId = pm.Id, StartDate = DateTime.UtcNow.AddMonths(-1), DueDate = DateTime.UtcNow.AddMonths(2), Priority = TaskPriority.Medium, Status = ProjectTaskStatus.InProgress, Progress = 30 },
            new ProjectTask { Name = "Permit renewal", ProjectId = project5.Id, AssignedEmployeeId = pm.Id, StartDate = DateTime.UtcNow.AddMonths(-1), DueDate = DateTime.UtcNow.AddDays(10), Priority = TaskPriority.Urgent, Status = ProjectTaskStatus.Pending, Progress = 5 }
        );

        await context.SaveChangesAsync();

        // Sample documents (real, downloadable PDF files, one of each document type)
        var uploadsFolder = Path.Combine(webRoot, "uploads", "documents");
        Directory.CreateDirectory(uploadsFolder);

        var saleForContract = sales[0];
        var rentalForContract = rentals[0];
        var firstPayment = await context.Payments.OrderBy(p => p.Id).FirstAsync();

        var seedDocuments = new List<Document>
        {
            CreateSeedDocument(uploadsFolder, admin.Id,
                title: "Sales Contract - Sale #" + saleForContract.Id,
                type: DocumentType.SalesContract,
                bodyLines: new[]
                {
                    $"Sale Reference: #{saleForContract.Id}",
                    $"Property: {ByCode("P-001").Code}",
                    $"Final Price: {saleForContract.FinalPrice:C0}",
                    $"Sale Date: {saleForContract.SaleDate:d}"
                },
                saleId: saleForContract.Id),

            CreateSeedDocument(uploadsFolder, admin.Id,
                title: "Rental Contract - Rental #" + rentalForContract.Id,
                type: DocumentType.RentalContract,
                bodyLines: new[]
                {
                    $"Rental Reference: #{rentalForContract.Id}",
                    $"Monthly Rent: {rentalForContract.MonthlyRent:C0}",
                    $"Start Date: {rentalForContract.StartDate:d}",
                    $"End Date: {rentalForContract.EndDate:d}"
                },
                rentalId: rentalForContract.Id),

            CreateSeedDocument(uploadsFolder, admin.Id,
                title: "Property Document - " + ByCode("P-001").Code,
                type: DocumentType.PropertyDocument,
                bodyLines: new[]
                {
                    $"Property Code: {ByCode("P-001").Code}",
                    $"Type: {ByCode("P-001").Type}",
                    $"Area: {ByCode("P-001").Area} sqm"
                },
                propertyId: ByCode("P-001").Id),

            CreateSeedDocument(uploadsFolder, admin.Id,
                title: "Project Document - " + project1.Name,
                type: DocumentType.ProjectDocument,
                bodyLines: new[]
                {
                    $"Project: {project1.Name}",
                    $"Location: {project1.Location}",
                    $"Budget: {project1.Budget:C0}"
                },
                projectId: project1.Id),

            CreateSeedDocument(uploadsFolder, admin.Id,
                title: "Payment Receipt - Payment #" + firstPayment.Id,
                type: DocumentType.PaymentReceipt,
                bodyLines: new[]
                {
                    $"Payment Reference: #{firstPayment.Id}",
                    $"Amount: {firstPayment.Amount:C0}",
                    $"Method: {firstPayment.Method}",
                    $"Date: {firstPayment.PaymentDate:d}"
                },
                customerId: firstPayment.CustomerId),
        };

        context.Documents.AddRange(seedDocuments);
        await context.SaveChangesAsync();
    }

    private static string CreatePlaceholderPropertyImage(string folder, string propertyCode, PropertyType type)
    {
        var (background, icon) = type switch
        {
            PropertyType.Apartment => ("#3b6fd6", "\U0001F3E2"),
            PropertyType.Villa => ("#1a9d5e", "\U0001F3E1"),
            PropertyType.Office => ("#8a5cf5", "\U0001F3E2"),
            PropertyType.Shop => ("#b8860b", "\U0001F3EA"),
            PropertyType.Land => ("#d9463f", "\U0001F33F"),
            _ => ("#666f80", "\U0001F3E0")
        };

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="400" height="300" viewBox="0 0 400 300">
                <rect width="400" height="300" fill="{background}" />
                <text x="200" y="140" font-size="64" text-anchor="middle" dominant-baseline="middle">{icon}</text>
                <text x="200" y="220" font-size="28" font-family="Segoe UI, sans-serif" fill="#ffffff" text-anchor="middle" font-weight="bold">{propertyCode}</text>
                <text x="200" y="250" font-size="16" font-family="Segoe UI, sans-serif" fill="#ffffffcc" text-anchor="middle">{type}</text>
            </svg>
            """;

        var fileName = $"{Guid.NewGuid()}.svg";
        File.WriteAllText(Path.Combine(folder, fileName), svg);
        return $"/uploads/properties/{fileName}";
    }

    private static Document CreateSeedDocument(string uploadsFolder, string uploadedById, string title, DocumentType type,
        string[] bodyLines, int? projectId = null, int? propertyId = null, int? saleId = null, int? rentalId = null, int? customerId = null)
    {
        var fileName = $"{Guid.NewGuid()}.pdf";
        var fullPath = Path.Combine(uploadsFolder, fileName);
        long fileSize = 0;
        try
        {
            var pdfBytes = Services.SeedDocumentPdfGenerator.Generate(title, bodyLines);
            File.WriteAllBytes(fullPath, pdfBytes);
            fileSize = pdfBytes.LongLength;
        }
        catch
        {
            var fallback = System.Text.Encoding.UTF8.GetBytes($"%PDF-1.4\n{title}\n" + string.Join("\n", bodyLines));
            File.WriteAllBytes(fullPath, fallback);
            fileSize = fallback.LongLength;
        }

        return new Document
        {
            Title = title,
            Type = type,
            FilePath = $"/uploads/documents/{fileName}",
            FileName = title.Replace(" ", "_") + ".pdf",
            FileSize = fileSize,
            ProjectId = projectId,
            PropertyId = propertyId,
            SaleId = saleId,
            RentalId = rentalId,
            CustomerId = customerId,
            UploadedById = uploadedById,
            UploadedDate = DateTime.UtcNow
        };
    }

    private static async Task SeedDefaultPermissionsAsync(ApplicationDbContext context)
    {
        if (await context.RolePermissions.AnyAsync()) return;

        // Mirrors the module access each role had before permissions became admin-configurable.
        var defaults = new (string Role, string[] Modules)[]
        {
            (Roles.ProjectManager, new[]
            {
                Modules.Projects, Modules.Properties, Modules.Customers,
                Modules.Sales, Modules.Rentals, Modules.Payments,
                Modules.Tasks, Modules.Documents, Modules.Reports,
                Modules.BookingRequests
            }),
            (Roles.SalesAgent, new[]
            {
                Modules.Properties, Modules.Customers, Modules.Sales,
                Modules.Rentals, Modules.Documents, Modules.BookingRequests
            }),
            (Roles.Accountant, new[]
            {
                Modules.Properties, Modules.Customers, Modules.Sales,
                Modules.Rentals, Modules.Payments, Modules.Documents,
                Modules.Reports
            }),
        };

        foreach (var (role, allowedModules) in defaults)
        {
            foreach (var (key, _, _) in Modules.All)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    Role = role,
                    Module = key,
                    IsAllowed = allowedModules.Contains(key)
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new Exception($"Failed to create seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        return user;
    }
}
