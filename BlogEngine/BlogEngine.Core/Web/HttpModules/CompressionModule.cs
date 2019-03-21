#region Using

using System;
using System.Net;
using System.Web;
using System.Web.Caching;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

#endregion

namespace BlogEngine.Core.Web.HttpModules
{
	/// <summary>
	/// Compresses the output using standard gzip/deflate.
	/// </summary>
	public sealed class CompressionModule : IHttpModule
	{

		#region IHttpModule Members

		/// <summary>
		/// Disposes of the resources (other than memory) used by the module 
		/// that implements <see cref="T:System.Web.IHttpModule"></see>.
		/// </summary>
		void IHttpModule.Dispose()
		{
			// Nothing to dispose; 
		}

		/// <summary>
		/// Initializes a module and prepares it to handle requests.
		/// </summary>
		/// <param name="context">An <see cref="T:System.Web.HttpApplication"></see> 
		/// that provides access to the methods, properties, and events common to 
		/// all application objects within an ASP.NET application.
		/// </param>
		void IHttpModule.Init(HttpApplication context)
		{
			if (BlogSettings.Instance.EnableHttpCompression)
			{
				context.PreRequestHandlerExecute += new EventHandler(context_PostReleaseRequestState);
			}
		}

		#endregion

		private const string GZIP = "gzip";
		private const string DEFLATE = "deflate";

		#region Compress page

		/// <summary>
		/// Handles the BeginRequest event of the context control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		void context_PostReleaseRequestState(object sender, EventArgs e)
		{
			HttpApplication app = (HttpApplication)sender;
			if (app.Context.CurrentHandler is System.Web.UI.Page && app.Request["HTTP_X_MICROSOFTAJAX"] == null)
			{
				if (IsEncodingAccepted(DEFLATE))
				{
					app.Response.Filter = new DeflateStream(app.Response.Filter, CompressionMode.Compress);
					SetEncoding(DEFLATE);
				}
				else if (IsEncodingAccepted(GZIP))
				{
					app.Response.Filter = new GZipStream(app.Response.Filter, CompressionMode.Compress);
					SetEncoding(GZIP);
				}

				if (BlogSettings.Instance.CompressWebResource)
					app.Response.Filter = new WebResourceFilter(app.Response.Filter);
			}
			else if (!BlogSettings.Instance.CompressWebResource && app.Context.Request.Path.Contains("WebResource.axd"))
			{
				app.Context.Response.Cache.SetExpires(DateTime.Now.AddDays(30));
			}
		}

		/// <summary>
		/// Checks the request headers to see if the specified
		/// encoding is accepted by the client.
		/// </summary>
		private static bool IsEncodingAccepted(string encoding)
		{
			HttpContext context = HttpContext.Current;
			return context.Request.Headers["Accept-encoding"] != null && context.Request.Headers["Accept-encoding"].Contains(encoding);
		}

		/// <summary>
		/// Adds the specified encoding to the response headers.
		/// </summary>
		/// <param name="encoding"></param>
		private static void SetEncoding(string encoding)
		{
			HttpContext.Current.Response.AppendHeader("Content-encoding", encoding);
		}

		#endregion

		#region WebResourceFilter

		private class WebResourceFilter : Stream
		{

			public WebResourceFilter(Stream sink)
			{
				_sink = sink;
			}

			private Stream _sink;

			#region Properites

			public override bool CanRead
			{
				get { return true; }
			}

			public override bool CanSeek
			{
				get { return true; }
			}

			public override bool CanWrite
			{
				get { return true; }
			}

			public override void Flush()
			{
				_sink.Flush();
			}

			public override long Length
			{
				get { return 0; }
			}

			private long _position;
			public override long Position
			{
				get { return _position; }
				set { _position = value; }
			}

			#endregion

			#region Methods

			public override int Read(byte[] buffer, int offset, int count)
			{
				return _sink.Read(buffer, offset, count);
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return _sink.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				_sink.SetLength(value);
			}

			public override void Close()
			{
				_sink.Close();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				byte[] data = new byte[count];
				Buffer.BlockCopy(buffer, offset, data, 0, count);
				string html = System.Text.Encoding.Default.GetString(buffer);

				Regex regex = new Regex("<script\\s*src=\"((?=[^\"]*webresource.axd)[^\"]*)\"\\s*type=\"text/javascript\"[^>]*>[^<]*(?:</script>)?", RegexOptions.IgnoreCase);
				foreach (Match match in regex.Matches(html))
				{
					string relative = match.Groups[1].Value;
					string absolute = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
					html = html.Replace(relative, Utils.RelativeWebRoot + "js.axd?path=" + HttpUtility.UrlEncode(absolute + relative));
				}

				byte[] outdata = System.Text.Encoding.Default.GetBytes(html);
				_sink.Write(outdata, 0, outdata.GetLength(0));
			}

			#endregion

		}

		#endregion

	}
}