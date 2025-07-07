using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.API.Contracts
{
    public record UserCreatedEvent(int Id, string Name, string Email);

}