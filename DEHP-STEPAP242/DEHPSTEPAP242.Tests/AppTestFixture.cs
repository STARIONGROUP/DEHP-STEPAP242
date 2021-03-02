

namespace DEHPSTEPAP242.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using Autofac;

    using DEHPCommon;

    using NUnit.Framework;
    
    [TestFixture]
    public class AppTestFixture
    {
        [Test]
        public void VerifyContainerIsBuilt()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<List<byte>>().As<IList>();
            Assert.IsNotNull(new App(containerBuilder));
            Assert.IsNotNull(AppContainer.Container.Resolve<IList>());
        }
    }
}
