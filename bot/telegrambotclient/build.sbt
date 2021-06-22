name := "telegrambotclient"

version := "0.1"

scalaVersion := "2.13.6"

idePackagePrefix := Some("com.planningpoker")

libraryDependencies ++= Seq(
  "com.bot4s" %% "telegram-core" % "5.0.0",
  "com.typesafe.akka" %% "akka-actor" % "2.5.32",
  "com.bot4s" %% "telegram-akka" % "5.0.0",
  "org.scalactic" %% "scalactic" % "3.2.9",
  "org.scalatest" %% "scalatest" % "3.2.9" % "test",
  "com.softwaremill.sttp.client3" %% "core" % "3.2.3"
)