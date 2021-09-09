import sbt._
name := "OrderingRule"
version := "0.1"
scalaVersion := "2.13.6"

val AkkaVersion = "2.6.8"
val AkkaHttpVersion = "10.2.6"

libraryDependencies ++= Seq(
    "org.scalatest" %% "scalatest" % "3.2.8" % Test,
    "org.typelevel" %% "cats-core" % "2.3.0",
    "com.typesafe.akka" %% "akka-actor-typed" % AkkaVersion,
    "com.typesafe.akka" %% "akka-stream" % AkkaVersion,
    "com.typesafe.akka" %% "akka-persistence-typed" % AkkaVersion,
    "com.typesafe.akka" %% "akka-persistence-testkit" % AkkaVersion % Test,
    "com.typesafe.akka" %% "akka-http" % AkkaHttpVersion,
    "com.fasterxml.jackson.module" % "jackson-module-scala_2.13" % "2.12.5"
)



// See https://www.scala-sbt.org/1.x/docs/Using-Sonatype.html for instructions on how to publish to Sonatype.
