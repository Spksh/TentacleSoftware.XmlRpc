using System;
using System.Collections.Generic;
using TentacleSoftware.XmlRpc.Core;

namespace TentacleSoftware.XmlRpc.Sample
{
    public class SampleResponder
    {
        [XmlRpcMethod("blogger.getUsersBlogs")]
        [XmlRpcMethod("metaWeblog.getUsersBlogs")]
        public List<Blog> GetUsersBlogs(string appKey, string username, string password)
        {
            Console.WriteLine("GetUsersBlogs");
            Console.WriteLine(" - " + appKey);
            Console.WriteLine(" - " + username);
            Console.WriteLine(" - " + password);

            return new List<Blog>
            {
                new Blog
                {
                    BlogId = "1",
                    BlogName = "My Blog 1",
                    Url = "http://myblog1.com"
                }
            };
        }

        [XmlRpcMethod("blogger.getAllBlogs")]
        [XmlRpcMethod("metaWeblog.getAllBlogs")]
        public List<Blog> GetAllBlogs()
        {
            Console.WriteLine("GetAllBlogs");

            return new List<Blog>
            {
                new Blog
                {
                    BlogId = "1",
                    BlogName = "My Blog 1",
                    Url = "http://myblog1.com"
                },
                new Blog
                {
                    BlogId = "2",
                    BlogName = "My Blog 2",
                    Url = "http://myblog2.com"
                }
            };
        }

        [XmlRpcMethod("blogger.addBlog")]
        [XmlRpcMethod("metaWeblog.addBlog")]
        public string AddBlog(Blog blog)
        {
            Console.WriteLine("AddBlog");
            Console.WriteLine(" - " + blog.BlogId);
            Console.WriteLine(" - " + blog.BlogName);
            Console.WriteLine(" - " + blog.Url);

            return blog.BlogId;
        }

        [XmlRpcMethod("blogger.addBlogs")]
        [XmlRpcMethod("metaWeblog.addBlogs")]
        public void AddBlogs(List<Blog> blogs)
        {
            Console.WriteLine("AddBlogs");

            foreach (Blog blog in blogs)
            {
                Console.WriteLine(" - " + blog.BlogId);
                Console.WriteLine(" - " + blog.BlogName);
                Console.WriteLine(" - " + blog.Url);
                Console.WriteLine();
            }
        }

        [XmlRpcMethod("blogger.addUsers")]
        [XmlRpcMethod("metaWeblog.addUsers")]
        public void AddUsers(params User[] users)
        {
            Console.WriteLine("AddUsers");

            foreach (User user in users)
            {
                Console.WriteLine(" - " + user.UserId);
                Console.WriteLine(" - " + user.Name);
                Console.WriteLine();
            }
        }
    }
}
