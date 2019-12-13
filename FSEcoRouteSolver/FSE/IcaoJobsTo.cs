// <copyright file="IcaoJobsTo.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.FSE
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    [XmlRoot(ElementName = "IcaoJobsTo", Namespace = "http://server.fseconomy.net")]
    public sealed class IcaoJobsTo : List<Assignment>
    {
    }
}
