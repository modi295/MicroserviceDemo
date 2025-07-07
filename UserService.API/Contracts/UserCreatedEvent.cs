using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UserService.API.Contracts
{
    public record UserCreatedEvent(int Id, string Name, string Email);

}