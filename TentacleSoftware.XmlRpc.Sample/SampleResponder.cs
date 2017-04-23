using System;
using System.Collections.Generic;
using System.Linq;
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
                    Url = "http://myblog1.com",
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
                    Url = "http://myblog1.com",
                    Users = new List<User>
                    {
                        new User
                        {
                            UserId = 1,
                            Name = "User 1"
                        },
                        new User
                        {
                            UserId = 2,
                            Name = "User 2"
                        }
                    }
                },
                new Blog
                {
                    BlogId = "2",
                    BlogName = "My Blog 2",
                    Url = "http://myblog2.com",
                    Users = new List<User>
                    {
                        new User
                        {
                            UserId = 1,
                            Name = "User 1"
                        },
                        new User
                        {
                            UserId = 2,
                            Name = "User 2"
                        }
                    }
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
        public List<string> AddBlogs(List<Blog> blogs)
        {
            Console.WriteLine("AddBlogs");

            foreach (Blog blog in blogs)
            {
                Console.WriteLine(" - " + blog.BlogId);
                Console.WriteLine(" - " + blog.BlogName);
                Console.WriteLine(" - " + blog.Url);
                Console.WriteLine();
            }

            return blogs.Select(b => b.BlogId).ToList();
        }

        [XmlRpcMethod("blogger.addUsers")]
        [XmlRpcMethod("metaWeblog.addUsers")]
        public List<int> AddUsers(params User[] users)
        {
            Console.WriteLine("AddUsers");

            foreach (User user in users)
            {
                Console.WriteLine(" - " + user.UserId);
                Console.WriteLine(" - " + user.Name);
                Console.WriteLine();
            }

            return users.Select(u => u.UserId).ToList();
        }
    }
}
