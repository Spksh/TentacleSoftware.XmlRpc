using System.Collections;
using NUnit.Framework;

namespace TentacleSoftware.XmlRpc.Test
{
    public class MethodCallTestData
    {
        public static IEnumerable TestCases
        {
            get
            {
                // Too many params
                yield return new TestCaseData(
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <methodCall>
                        <methodName>blogger.getUsersBlogs</methodName>
                        <params>
                            <param>
                                <value>
                                    <string>0123456789ABCDEF</string>
                                </value>
                            </param>
                            <param>
                                <value>
                                    <string>user</string>
                                </value>
                            </param>
                            <param>
                                <value>
                                    <string>P@ssw0rd</string>
                                </value>
                            </param>
                            <param>
                                <value>Unexpected parameter</value>
                            </param>
                        </params>
                    </methodCall>")
                    { TestName = "Too Many Params" };

                // Correct params
                yield return new TestCaseData(
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <methodCall>
                        <methodName>blogger.getUsersBlogs</methodName>
                        <params>
                            <param>
                                <value>
                                    <string>0123456789ABCDEF</string>
                                </value>
                            </param>
                            <param>
                                <value>user</value>
                            </param>
                            <param>
                                <value>
                                    <string>P@ssw0rd</string>
                                </value>
                            </param>
                        </params>
                    </methodCall>")
                    { TestName = "Correct Params" };

                // Correct params, async
                yield return new TestCaseData(
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <methodCall>
                        <methodName>blogger.getUsersBlogsAsync</methodName>
                        <params>
                            <param>
                                <value>
                                    <string>0123456789ABCDEF</string>
                                </value>
                            </param>
                            <param>
                                <value>user</value>
                            </param>
                            <param>
                                <value>
                                    <string>P@ssw0rd</string>
                                </value>
                            </param>
                        </params>
                    </methodCall>")
                    { TestName = "Correct Params Async" };

                // Empty params, and we're calling a method with no params
                yield return new TestCaseData(
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <methodCall>
                        <methodName>blogger.getAllBlogs</methodName>
                        <params>
                        </params>
                    </methodCall>")
                    { TestName = "Empty params" };

                // No params, and we're calling a method with no params
                yield return new TestCaseData(
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <methodCall>
                        <methodName>blogger.getAllBlogs</methodName>
                    </methodCall>")
                    { TestName = "No params" };

                // One struct
                yield return new TestCaseData(
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <methodCall>
                        <methodName>blogger.addBlog</methodName>
                        <params>
                            <param>
                                <value>
                                    <struct>
                                        <member>
                                            <name>blogid</name>
                                            <value><string>1</string></value>
                                        </member>
                                        <member>
                                            <name>blogName</name>
                                            <value><string>My Awesome Blog</string></value>
                                        </member>
                                        <member>
                                            <name>url</name>
                                            <value><string>https://myawesomeblog.com</string></value>
                                        </member>
                                    </struct>
                                </value>
                            </param>
                        </params>
                    </methodCall>")
                    { TestName = "One struct" };

                // Array of structs with nested structs
                yield return new TestCaseData(
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <methodCall>
                        <methodName>blogger.addBlogs</methodName>
                        <params>
                            <param>
                                <value>
                                    <array>
                                        <data>
                                            <value>
                                                <struct>
                                                    <member>
                                                        <name>blogid</name>
                                                        <value><string>1</string></value>
                                                    </member>
                                                    <member>
                                                        <name>blogName</name>
                                                        <value><string>My Awesome Blog</string></value>
                                                    </member>
                                                    <member>
                                                        <name>url</name>
                                                        <value><string>https://myawesomeblog.com</string></value>
                                                    </member>
                                                    <member>
                                                        <name>users</name>
                                                        <value>
                                                            <array>
                                                                <data>
                                                                    <value>
                                                                        <struct>
                                                                            <member>
                                                                                <name>userId</name>
                                                                                <value><i4>2</i4></value>
                                                                            </member>
                                                                            <member>
                                                                                <name>name</name>
                                                                                <value><string>User 2</string></value>
                                                                            </member>
                                                                        </struct>
                                                                    </value>
                                                                    <value>
                                                                        <struct>
                                                                            <member>
                                                                                <name>userId</name>
                                                                                <value><i4>3</i4></value>
                                                                            </member>
                                                                            <member>
                                                                                <name>name</name>
                                                                                <value><string>User 3</string></value>
                                                                            </member>
                                                                        </struct>
                                                                    </value>
                                                                </data>
                                                            </array>
                                                        </value>
                                                    </member>
                                                </struct>
                                            </value>
                                            <value>
                                                <struct>
                                                    <member>
                                                        <name>blogid</name>
                                                        <value><string>2</string></value>
                                                    </member>
                                                    <member>
                                                        <name>blogName</name>
                                                        <value><string>My Awesome Blog 2</string></value>
                                                    </member>
                                                    <member>
                                                        <name>url</name>
                                                        <value><string>https://myawesomeblog2.com</string></value>
                                                    </member>
                                                </struct>
                                            </value>
                                        </data>
                                    </array>
                                </value>
                            </param>
                        </params>
                    </methodCall>")
                    { TestName = "Array of nested structs" };

                yield return new TestCaseData(
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <methodCall>
                        <methodName>blogger.addUsers</methodName>
                        <params>
                            <param>
                                <value>
                                    <array>
                                        <data>
                                            <value>
                                                <struct>
                                                    <member>
                                                        <name>userId</name>
                                                        <value><i4>2</i4></value>
                                                    </member>
                                                    <member>
                                                        <name>name</name>
                                                        <value><string>User 2</string></value>
                                                    </member>
                                                </struct>
                                            </value>
                                            <value>
                                                <struct>
                                                    <member>
                                                        <name>userId</name>
                                                        <value><i4>3</i4></value>
                                                    </member>
                                                    <member>
                                                        <name>name</name>
                                                        <value><string>User 3</string></value>
                                                    </member>
                                                </struct>
                                            </value>
                                            <value>
                                                <struct>
                                                    <member>
                                                        <name>userId</name>
                                                        <value><i4>3</i4></value>
                                                    </member>
                                                    <member>
                                                        <name>name</name>
                                                        <value><string>User 3</string></value>
                                                    </member>
                                                </struct>
                                            </value>
                                            <value>
                                                <struct>
                                                    <member>
                                                        <name>userId</name>
                                                        <value><i4>3</i4></value>
                                                    </member>
                                                    <member>
                                                        <name>name</name>
                                                        <value><string>User 3</string></value>
                                                    </member>
                                                </struct>
                                            </value>
                                            <value>
                                                <struct>
                                                    <member>
                                                        <name>userId</name>
                                                        <value><i4>7</i4></value>
                                                    </member>
                                                    <member>
                                                        <name>name</name>
                                                        <value><string>User 7</string></value>
                                                    </member>
                                                </struct>
                                            </value>
                                        </data>
                                    </array>
                                </value>
                            </param>
                        </params>
                    </methodCall>")
                    { TestName = "Many structs" };

                // methodResponse, not a methodCall
                yield return new TestCaseData(
                    @"<?xml version = ""1.0"" ?>
                    <methodResponse>
                        <params>
                            <param>
                                <value>
                                    <array>
                                        <data>
                                            <value>
                                                <struct>
                                                    <member>
                                                        <name>categoryid</name>
                                                        <value><i4>1</i4></value>
                                                    </member>
                                                    <member>
                                                        <name>title</name>
                                                        <value><string>Category 1</string></value>
                                                    </member>
                                                    <member>
                                                        <name>description</name>
                                                        <value><boolean>true</boolean></value>
                                                    </member>
                                                </struct>
                                            </value>
                                            <value>
                                                <struct>
                                                    <member>
                                                        <name>categoryid</name>
                                                        <value><i4>2</i4></value>
                                                    </member>
                                                    <member>
                                                        <name>title</name>
                                                        <value><string>Category 2</string></value>
                                                    </member>
                                                    <member>
                                                        <name>description</name>
                                                        <value><boolean>false</boolean></value>
                                                    </member>
                                                </struct>
                                            </value>
                                        </data>
                                    </array>
                                </value>    
                            </param>
                        </params>
                    </methodResponse>")
                    { TestName = "methodResponse, not a methodCall" };
            }
        }
    }
}
