/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System;
  using System.Linq;
  using System.Web.Mvc;
  using System.Web.Routing;
  using Ninject;

  public class DIControllerFactory : DefaultControllerFactory
  {
    private readonly IKernel kernel;
    public DIControllerFactory(IKernel kernel)
    {
      this.kernel = kernel;
    }

    protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
    {
      if (!this.kernel.GetBindings(controllerType).Any())
      {
        this.kernel.Bind(controllerType).To(controllerType);
      }
      return (IController)this.kernel.Get(controllerType);
    }
  }
}