using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketsServicios.Api.Models
{
    public class SocketModel
    {
        public Guid To { get; set; }

        public string From { get; set; }

        public string Message { get; set; }
         
    }

}
