// Write your Javascript code.
console.log("hello!!!");

$.ajax({
    type: "GET",
    url: "https://api.github.com/repos/ucdavis/FeedbackBot/issues/1",
    dataType: "json",
    success: function(data) {
        // Debugging purposes
        console.log(data);
    },
    error: function(data) {
        console.log(data);
    }
  });
