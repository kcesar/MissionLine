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
    private readonly object bindingLock = new object();

    public DIControllerFactory(IKernel kernel)
    {
      this.kernel = kernel;
    }

    protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
    {
      lock (bindingLock)
      {
        if (!kernel.GetBindings(controllerType).Any())
        {
          kernel.Bind(controllerType).To(controllerType);
        }
      }
      return (IController)kernel.Get(controllerType);
    }
  }
}