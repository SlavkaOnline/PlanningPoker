name := "TelegramBot"

version := "0.1"

scalaVersion := "2.13.5"

idePackagePrefix := Some("planning.poker")


val AkkaVersion = "2.6.15"

libraryDependencies ++= Seq(
    "org.typelevel" %% "cats-core" % "2.3.0",
    "com.typesafe.akka" %% "akka-actor-typed" % AkkaVersion,
    "com.typesafe.akka" %% "akka-slf4j" % AkkaVersion,
    "ch.qos.logback" % "logback-classic" % "1.2.3",
    "org.scala-lang.modules" %% "scala-parser-combinators" % "1.1.2",
    "com.bot4s" %% "telegram-core" % "5.0.0",
    "com.microsoft.signalr" % "signalr" % "5.0.0",
    "org.http4s" %% "http4s-blaze-client" % "0.21.24",
    "org.http4s" %% "http4s-dsl" % "0.21.24",
    "org.http4s" %% "http4s-circe" % "0.21.24",
    "io.circe" %% "circe-generic" % "0.14.1",
    "io.circe" %% "circe-literal" % "0.14.1"
)