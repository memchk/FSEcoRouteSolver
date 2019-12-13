// <copyright file="ApiException.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.FSE
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ApiException<TResult> : Exception
    {
        public ApiException(TResult res)
        {
            this.Result = res;
        }

        public TResult Result { get; set; }
    }
}
