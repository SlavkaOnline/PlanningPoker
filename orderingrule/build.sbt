import sbt._
name := "OrderingRule"
version := "0.1"
scalaVersion := "2.13.6"

val AkkaVersion = "2.6.16"
val AkkaHttpVersion = "10.2.6"

libraryDependencies ++= Seq(
    "org.scalatest" %% "scalatest" % "3.2.9" % Test,
    "org.typelevel" %% "cats-core" % "2.6.1",
    "com.typesafe.akka" %% "akka-actor-typed" % AkkaVersion,
    "com.typesafe.akka" %% "akka-stream" % AkkaVersion,
    "com.typesafe.akka" %% "akka-persistence-typed" % AkkaVersion,
    "com.typesafe.akka" %% "akka-persistence-testkit" % AkkaVersion % Test,
    "com.typesafe.akka" %% "akka-http" % AkkaHttpVersion,
    "com.softwaremill.sttp.tapir" %% "tapir-akka-http-server" % "0.18.3",
    "ch.qos.logback" % "logback-classic" % "1.2.6",
    "com.softwaremill.sttp.tapir" %% "tapir-json-circe" % "0.18.3",
    "com.softwaremill.sttp.tapir" %% "tapir-swagger-ui-akka-http" % "0.18.3" exclude("com.typesafe.akka", "akka-stream_2.12"),
    "com.softwaremill.sttp.tapir" %% "tapir-openapi-docs" % "0.18.3",
    "com.softwaremill.sttp.tapir" %% "tapir-redoc-akka-http" % "0.18.3",
    "com.softwaremill.sttp.tapir" %% "tapir-openapi-circe-yaml" % "0.18.3"
)



// See https://www.scala-sbt.org/1.x/docs/Using-Sonatype.html for instructions on how to publish to Sonatype.
