var password = args.Length > 0 ? args[0] : "Admin@123";
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(password));
