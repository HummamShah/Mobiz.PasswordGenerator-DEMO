# Mobiz.PasswordGenerator

The project contains 3 azure functions.
1- Generating Password.
2- Rate Limiting
3- Process delayed requests.

Clone the project and run it directly.

**Endpoints.**

1)http://localhost:7121/api/Passwordgenerator?numberOfPasswords=1&passwordLength=2

2)http://localhost:7121/api/RateLimitWithFile?userId=123

Was having issues with my azure account so I have used RateLimitWithFile instead of RateLimit which uses local file instead of connection string of azure cosmo db.
For demo purpose I have created separate function for rate limit check, but we can merge 1- Generating Password and 2- Rate Limiting so a single endpoint is can be called for both.
