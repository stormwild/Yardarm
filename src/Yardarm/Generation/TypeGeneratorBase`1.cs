﻿using System;
using Microsoft.OpenApi.Interfaces;
using Yardarm.Spec;

namespace Yardarm.Generation
{
    public abstract class TypeGeneratorBase<T> : TypeGeneratorBase
        where T : IOpenApiElement
    {
        protected LocatedOpenApiElement<T> Element { get; }

        protected TypeGeneratorBase(LocatedOpenApiElement<T> element, GenerationContext context)
            : base(context)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
        }
    }
}
