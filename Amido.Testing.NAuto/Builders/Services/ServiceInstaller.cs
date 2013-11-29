﻿namespace Amido.Testing.NAuto.Builders.Services
{
    public static class ServiceInstaller
    {
        public static void Install(Nject.NAutoContainer container)
        {
            container.Register<IPropertyPopulationService, PropertyPopulationService>();
            container.Register<PopulateProperty<string>, PopulateStringService>();
            container.Register<IPopulateEnumService, PopulateEnumService>();
        }
    }
}