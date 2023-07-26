using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

public class User
{
    public string Email { get; set; }
    public string FullName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public DateTime Date { get; set; }
    public string Token { get; set; }
}

public class Function
{
    private readonly IAmazonS3 _s3Client;
    private const string S3BucketName = "YOUR_S3_BUCKET_NAME";
    private const string S3ObjectKey = "YOUR_S3_OBJECT_KEY";

    public Function(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
    {
        try
        {
            var requestBody = JsonConvert.DeserializeObject<User>(input.Body);

            // Read the existing S3 object and deserialize it
            var s3Object = ReadS3Object();
            var users = JsonConvert.DeserializeObject<List<User>>(s3Object);

            // Find the user with the matching email
            var existingUser = users.FirstOrDefault(u => u.Email == requestBody.Email);

            if (existingUser != null)
            {
                // Update user and password
                existingUser.Username = requestBody.Username;
                existingUser.Password = requestBody.Password;
                // Initialize date and token
                existingUser.Date = DateTime.Now;
                existingUser.Token = GenerateToken();

                // Save the updated list of users back to S3
                SaveUsersToS3(users);

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = JsonConvert.SerializeObject(new { Message = "User updated successfully.", User = existingUser })
                };
            }
            else
            {
                // Insert new record
                requestBody.Date = DateTime.Now;
                requestBody.Token = GenerateToken();
                users.Add(requestBody);

                // Save the updated list of users back to S3
                SaveUsersToS3(users);

                return new APIGatewayProxyResponse
                {
                    StatusCode = 201,
                    Body = JsonConvert.SerializeObject(new { Message = "User inserted successfully.", User = requestBody })
                };
            }
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonConvert.SerializeObject(new { Message = "Error retrieving/updating S3 object.", Error = ex.Message })
            };
        }
    }

    private string ReadS3Object()
    {
        GetObjectRequest request = new GetObjectRequest
        {
            BucketName = S3BucketName,
            Key = S3ObjectKey
        };

        using (GetObjectResponse response = _s3Client.GetObjectAsync(request).Result)
        using (Stream responseStream = response.ResponseStream)
        using (StreamReader reader = new StreamReader(responseStream))
        {
            return reader.ReadToEnd();
        }
    }

    private void SaveUsersToS3(List<User> users)
    {
        string serializedUsers = JsonConvert.SerializeObject(users);
        byte[] byteArray = Encoding.UTF8.GetBytes(serializedUsers);

        using (MemoryStream stream = new MemoryStream(byteArray))
        {
            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = S3BucketName,
                Key = S3ObjectKey,
                InputStream = stream
            };

            _s3Client.PutObjectAsync(request).Wait();
        }
    }

    private string GenerateToken()
    {
        // Your token generation logic here
        return Guid.NewGuid().ToString();
    }
}
