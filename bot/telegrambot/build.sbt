libraryDependencies ++= Seq(
  "org.scala-lang.modules" %% "scala-parser-combinators" % "1.1.2",
  "com.bot4s" %% "telegram-core" % "5.0.0",
  "com.microsoft.signalr" % "signalr" % "5.0.0",
  "com.typesafe.akka" %% "akka-stream" % "2.5.32",
  "org.typelevel" %% "cats-core" % "2.3.0"
)


lazy val root = (project in file(".")).
    settings(
        inThisBuild(List(
            organization := "com.planningpoker",
            scalaVersion := "2.13.5",
            version := "1.0"
        )),
        name := "planning-poker"
    )

// To learn more about multi-project builds, head over to the official sbt
// documentation at http://www.scala-sbt.org/documentation.html
