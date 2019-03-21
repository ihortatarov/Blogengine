#region Using

using System;
using System.Web;
using BlogEngine.Core;

#endregion

namespace BlogEngine.Core.Web.HttpHandlers
{
    /// <summary>
    /// 
    /// </summary>
    public class OpmlHandler : IHttpHandler
    {

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that 
        /// implements the <see cref="T:System.Web.IHttpHandler"></see> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"></see> 
        /// object that provides references to the intrinsic server objects 
        /// (for example, Request, Response, Session, and Server) used to service HTTP requests.
        /// </param>
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/xml";
            //context.Response.AppendHeader("Content-Disposition", "attachment; filename=opml.xml");
            //context.Response.TransmitFile(context.Request.PhysicalApplicationPath +  "App_Data/blogroll.xml");
            context.Response.TransmitFile(context.Server.MapPath(BlogSettings.Instance.StorageLocation) + "blogroll.xml");

        }

        /// <summary>
        /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"></see> instance.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler"></see> instance is reusable; otherwise, false.</returns>
        public bool IsReusable
        {
            get { return false; }
        }

    }
}