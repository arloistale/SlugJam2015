Parse.Cloud.define("SignUpCloud", function(request, response) {
  var user = new Parse.User();
  user.set("username", request.params.username);
  user.set("password", request.params.password);
  user.set("streak", 0);

  user.signUp(null, {
    success: function(user) {
      response.success({ "token": user.getSessionToken() });
    },
    error: function(user, error) {
      // Check for user session-related errors
      if (error.code >= 200 && error.code < 300) {
        // Unity Hack
        response.success({ "code": error.code, "message": error.message });
      } else {
        response.error({ "code": error.code, "message": error.message });
      }
    }
  });
});
