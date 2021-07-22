package planning.poker

import pocker.Client

import scala.io.StdIn.readLine

object Main extends App {
    Client.login(Client.LoginUser("Slava"))
        .unsafeRunAsync {
            case Right(user) => println(user)
            case Left(err) => println(err.getMessage)
        }
    readLine()
}
