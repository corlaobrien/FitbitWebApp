var heartRateData;
var userData;

// If user hasn't authed with Fitbit, redirect to Fitbit OAuth Implicit Grant Flow
var fitbitAccessToken;

var entriesList = [];

if (!window.location.hash) {
    //Prod
    window.location.replace('https://www.fitbit.com/oauth2/authorize?response_type=token&client_id=227ZVN&redirect_uri=http://52.211.161.75/FitbitWebsite/&scope=activity%20nutrition%20heartrate%20location%20nutrition%20profile%20settings%20sleep%20social%20weight');
    //Dev
    //window.location.replace('https://www.fitbit.com/oauth2/authorize?response_type=token&client_id=227YKP&redirect_uri=http://localhost:51302/&scope=activity%20nutrition%20heartrate%20location%20nutrition%20profile%20settings%20sleep%20social%20weight');
} else {
    var fragmentQueryParameters = {};
    window.location.hash.slice(1).replace(
        new RegExp("([^?=&]+)(=([^&]*))?", "g"),
        function ($0, $1, $2, $3) { fragmentQueryParameters[$1] = $3; }
    );

    fitbitAccessToken = fragmentQueryParameters.access_token;
}

// Make an API request and graph it
var processResponse = function (res) {
    if (!res.ok) {
        throw new Error('Fitbit API request failed: ' + res);
    }

    var contentType = res.headers.get('content-type')
    if (contentType && contentType.indexOf("application/json") !== -1) {
        return res.json();
    } else {
        throw new Error('JSON expected but received ' + contentType);
    }
}

var processHeartRate = function (timeSeries) {
    heartRateData = timeSeries;


    var entriesList = [];

    $.each(heartRateData['activities-heart-intraday'].dataset, function (i, d) {
        var time = d.time.split(":");
        var date = new Date("2016", "10", "23", time[0], time[1], time[2], "00");

        entriesList.push({
            userID: userData.userID,
            dateTime: date.toISOString(),
            name: "Heart Rate",
            value: d.value.toString()
        });
    });

    //entriesListString = JSON.stringify(entriesList);

    //$.each(entriesList, function (index, value) {
    $.ajax({
        url: $("html").data("root") + "Home/SaveEntries",
        type: "POST",
        contentType: "application/json;",
        datatype: "json",
        data: JSON.stringify({ tempTableListString: entriesList }),
        success: function (succes) {
            console.log("Success");
        },
        error: function (jqXHR, textStatus, errorThrown) {
            console.log("Error");
        }
    });
    //});


    //$.ajax({
    //    url: $("html").data("root") + "Home/SaveEntries",
    //    contentType: "application/json; charset=utf-8",
    //    type: "POST",
    //    dataType: "json",
    //    data: {
    //        tempTableList: entriesList
    //    },
    //    success: function () {
    //        console.log("Success");
    //    },
    //    error: function (jqXHR, textStatus, errorThrown) {
    //        console.log("Error");
    //    }
    //});

    //return heartRateData['activities-heart-intraday'].dataset.map(
    //    function(measurement) {
    //        return [
    //            measurement.time.split(':').map(
    //                function(timeSegment) {
    //                    return Number.parseInt(timeSegment);
    //                }
    //            ),
    //            measurement.value
    //        ];
    //    }
    //);
}

var processUser = function (timeSeries) {
    userData = timeSeries.user;

    $.ajax({
        url: $("html").data("root") + "Home/SaveUser",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        data: {
            userName: userData.fullName,
            firstName: "Orla",
            lastName: "OBrien",
            dob: userData.dateOfBirth
        },
        success: function (userID) {
            userData.userID = userID;

            fetch(
                    'https://api.fitbit.com/1/user/-/activities/heart/date/2016-10-08/1d/1sec.json',
                    {
                        headers: new Headers({
                            'Authorization': 'Bearer ' + fitbitAccessToken
                        }),
                        mode: 'cors',
                        method: 'GET'
                    })
                    //.then(function (responseHR) {
                    //    heartRateData = responseHR;
                    //})
                    //.then(

                        .then(processResponse)
                    .then(processHeartRate)
                    //.then(graphHeartRate)
                    .catch(function (error) {
                        console.log(error);
                    });
        },
        error: function (jqXHR, textStatus, errorThrown) {
            console.log("Error");
        }
    });
}

function saveEntries() {

    var entriesList = [];

    $.each(heartRateData['activities-heart-intraday'].dataset, function (i, d) {
        var time = d.time.split(":");
        var date = new Date("2016", "10", "23", time[0], time[1], time[2], "00");

        entriesList.push({
            userID: userData.userID,
            dateTime: date.toISOString(),
            name: "HR",
            value: d.value
        });
    });


    $.ajax({
        url: $("html").data("root") + "Home/SaveEntry",
        contentType: "application/json",
        type: "POST",
        dataType: "json",
        data: entriesList,
        success: function () {

            console.log("Sucess");
        },
        error: function (jqXHR, textStatus, errorThrown) {
            console.log("Error");
        }
    });


}

var graphHeartRate = function (timeSeries) {
    var n = 40,
    //random = d3.randomNormal(0, .2),
    data = entriesList;

    var svg = d3.select("svg"),
        margin = { top: 20, right: 20, bottom: 20, left: 40 },
        width = +svg.attr("width") - margin.left - margin.right,
        height = +svg.attr("height") - margin.top - margin.bottom,
        g = svg.append("g").attr("transform", "translate(" + margin.left + "," + margin.top + ")");

    var x = d3.scaleLinear()
        .domain([0, n - 1])
        .range([0, width]);

    var y = d3.scaleLinear()
        .domain([0, 200])
        .range([height, 0]);

    var line = d3.line()
        .x(function (d, i) { return x(i); })
        .y(function (d, i) { return y(d); });

    g.append("defs").append("clipPath")
        .attr("id", "clip")
      .append("rect")
        .attr("width", width)
        .attr("height", height);

    g.append("g")
        .attr("class", "axis axis--x")
        .attr("transform", "translate(0," + y(0) + ")")
        .call(d3.axisBottom(x));

    g.append("g")
        .attr("class", "axis axis--y")
        .call(d3.axisLeft(y));

    g.append("g")
        .attr("clip-path", "url(#clip)")
      .append("path")
        .datum(d3.select(data.value))
        .attr("class", "line")
      .transition()
        .duration(500)
        .ease(d3.easeLinear)
        .on("start", tick);



    //console.log(timeSeries);

    //// var myJsonString = JSON.stringify(timeSeries);
    //// console.log(myJsonString);

    //var data = new google.visualization.DataTable();
    //data.addColumn('timeofday', 'Time of Day');
    //data.addColumn('number', 'Heart Rate');

    //data.addRows(timeSeries);

    //var options = google.charts.Line.convertOptions({
    //    height: 450
    //});

    //var chart = new google.charts.Line(document.getElementById('chart'));

    //chart.draw(data, options);
}

function tick() {

    // Push a new data point onto the back.
    data.push(random());

    // Redraw the line.
    d3.select(this)
        .attr("d", line)
        .attr("transform", null);

    // Slide it to the left.
    d3.active(this)
        .attr("transform", "translate(" + x(-1) + ",0)")
      .transition()
        .on("start", tick);

    // Pop the old data point off the front.
    data.shift();

}




fetch(
    'https://api.fitbit.com/1/user/-/profile.json',
    {
        headers: new Headers({
            'Authorization': 'Bearer ' + fitbitAccessToken
        }),
        mode: 'cors',
        method: 'GET'
    })
    .then(processResponse)
    .then(processUser)
.catch(function (error) {
    console.log(error);
});

//.then(
//fetch(
//	'https://api.fitbit.com/1/user/-/activities/steps/date/today/1d.json',
//	{
//	    headers: new Headers({
//	        'Authorization': 'Bearer ' + fitbitAccessToken
//	    }),
//	    mode: 'cors',
//	    method: 'GET'
//	}
//)

//    //'https://api.fitbit.com/1/user/-/activities/heart/date/today/1d/1sec/time/00:01/11:00.json',