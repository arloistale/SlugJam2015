Parse.Cloud.define("SignUpCloud", function(request, response) {
  var user = new Parse.User();
  user.set("username", request.params.username);
  user.set("password", request.params.password);
  user.set("streak", 0);
  user.set("dailyStreak", 0);
  user.set("dailyTimestamp", new Date());

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

Parse.Cloud.define("SubmitStreak", function(request, response) {
  var user = request.user;
  var streak = request.params.streak;
  var dailyStreak = request.params.dailyStreak;
  var dailyTimestamp = request.params.dailyTimestamp;
  
  if(typeof(streak) != 'number' || typeof(dailyStreak) != 'number' || typeof(dailyTimestamp) == 'undefined')
    response.error("SubmitStreak: Invalid params");

  user.set("streak", streak);
  user.set("dailyStreak", dailyStreak);
  user.set("dailyTimestamp", dailyTimestamp);

  user.save(null, {
    success: function(savedUser) {
      response.success({ "token": savedUser.getSessionToken() });
    },
    error: function(savedUser, error) {
      response.error({ "code": error.code, "message": error.message });
    }
  });
});
