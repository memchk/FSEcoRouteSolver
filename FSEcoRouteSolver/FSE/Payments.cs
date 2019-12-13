// <copyright file="Payments.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.FSE
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot(ElementName = "PaymentsByMonthYear", Namespace = "http://server.fseconomy.net")]
    public sealed class Payments : List<Payment>
    {
    }
}
