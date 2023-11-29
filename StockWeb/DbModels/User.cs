using System;
using System.Collections.Generic;

namespace StockWeb.DbModels;

public partial class User
{
    public Guid UserId { get; set; }

    public string Account { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string UserName { get; set; } = null!;
}
