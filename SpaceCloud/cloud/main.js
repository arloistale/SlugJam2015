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

Parse.Cloud.define("SubmitStreak", function(request, response) {
  var user = request.user;
  var streak = request.params.streak;

  if(!streak)
    response.error("SubmitStreak: Invalid params");

  if(streak > user.get("streak"))
    user.set("streak", streak);

  var dailyTimestamp = user.get("dailyTimestamp");
  var currentDate = new Date();
  var isSameDay = (dailyTimestamp.getDate() == currentDate.getDate() 
        && dailyTimestamp.getMonth() == currentDate.getMonth()
        && dailyTimestamp.getFullYear() == currentDate.getFullYear());

  if(!isSameDay)
  {
    user.set("dailyStreak", 0);
  }

  if(streak > user.get("dailyStreak"))
    user.set("dailyStreak", streak);

  user.save(null, {
    success: function(savedUser) {
      response.success();
    },
    error: function(savedUser, error) {
      response.error({ "code": error.code, "message": error.message });
    }
  });
});
