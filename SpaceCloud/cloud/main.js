// functions

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
      response.error(error);
    }
  });
});

Parse.Cloud.define("FetchPhrases", function(request, response) {
  var requestLimit = request.params.requestLimit;  

  if(typeof(requestLimit) != 'number')
    response.error("FetchPhrases: Invalid params");

  var masterQuery = new Parse.Query("Master");
  masterQuery.first({
    success: function(master) {
      var count = master.get("phraseCount");
      var indexArr = [];
      for(var i = 0; i < count; ++i)
        indexArr[i] = i;
      shuffle(indexArr);
      indexArr = indexArr.slice(0, requestLimit);
      var query = new Parse.Query("Phrase");
      query.containedIn("index", indexArr);
      query.limit(requestLimit);
      query.find({
        success: function(results) {
          response.success(results);
        },
        error: function(error) {
          response.error(error);
        }
      }); 
    },
    error: function(error) {
      status.error("Unable to get phrase count: " + error.toString());
    }
  });
});

// jobs

Parse.Cloud.job("PhraseEnumeration", function(request, status) {
  // Query for the master
  var masterQuery = new Parse.Query("Master");
  masterQuery.first({
    success: function(master) {
      var index = 0;
      var query = new Parse.Query("Phrase");
      query.each(function(phrase) {
        // Set and save the change
        phrase.set("index", index);
        index++;
        return phrase.save();
      }).then(function() {
        master.set("phraseCount", index);
        master.save().then(function() {
          // Set the job's success status
          status.success("Enumeration completed successfully.");
        }, function(error) {
          status.error("Counting: " + error.toString());
        });
      }, function(error) {
        // Set the job's error status
        status.error("Phrase query: " + error.toString());
      });   
    },
    error: function(error) {
      status.error("Unable to index: " + error.toString());
    }
  });
});

function shuffle(array) {
  var currentIndex = array.length, temporaryValue, randomIndex ;

  // While there remain elements to shuffle...
  while (0 !== currentIndex) {

    // Pick a remaining element...
    randomIndex = Math.floor(Math.random() * currentIndex);
    currentIndex -= 1;

    // And swap it with the current element.
    temporaryValue = array[currentIndex];
    array[currentIndex] = array[randomIndex];
    array[randomIndex] = temporaryValue;
  }

  return array;
}
