using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeafComposer.Api.ErrorHandling
{
    public class ApiOKResponse : ApiResponse
    {
        public object Result { get; }

        public ApiOKResponse(object result) : base(200)
        {
            Result = result;
        }
    }
}
